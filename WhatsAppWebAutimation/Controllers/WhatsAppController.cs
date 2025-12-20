using Microsoft.AspNetCore.Mvc;
using WhatsAppWebAutomation.DTOs;
using WhatsAppWebAutomation.Services;

namespace WhatsAppWebAutomation.Controllers;

/// <summary>
/// WhatsApp Web Automation API Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(IWhatsAppService whatsAppService, ILogger<WhatsAppController> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    /// <summary>
    /// Check WhatsApp status (browser open, logged in)
    /// </summary>
    /// <returns>Current status of WhatsApp Web automation</returns>
    /// <response code="200">Returns the current status</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<StatusResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StatusResultDto>>> GetStatus()
    {
        _logger.LogInformation("Status check requested");

        var status = await _whatsAppService.GetStatusAsync();

        return Ok(ApiResponse<StatusResultDto>.SuccessResponse(status, status.Message));
    }

    /// <summary>
    /// Initialize browser and open WhatsApp Web for QR code scanning
    /// </summary>
    /// <param name="request">Optional initialization settings including browser type</param>
    /// <returns>Initialization status</returns>
    /// <response code="200">Browser initialized successfully</response>
    /// <response code="400">Invalid browser type</response>
    /// <response code="500">Failed to initialize browser</response>
    [HttpPost("init")]
    [ProducesResponseType(typeof(ApiResponse<StatusResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StatusResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<StatusResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<StatusResultDto>>> Initialize([FromBody] InitRequest? request = null)
    {
        var browserType = request?.BrowserType ?? "Chrome";
        _logger.LogInformation("Initialize browser requested with type: {BrowserType}", browserType);

        try
        {
            var status = await _whatsAppService.InitializeAsync(browserType);

            if (status.BrowserOpen)
            {
                return Ok(ApiResponse<StatusResultDto>.SuccessResponse(status, status.Message));
            }

            return StatusCode(500, ApiResponse<StatusResultDto>.ErrorResponse(
                "Failed to open browser",
                status.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid browser type requested: {BrowserType}", browserType);
            return BadRequest(ApiResponse<StatusResultDto>.ErrorResponse(
                ex.Message,
                "Invalid browser type"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing browser");
            return StatusCode(500, ApiResponse<StatusResultDto>.ErrorResponse(
                ex.Message,
                "Failed to initialize WhatsApp"));
        }
    }

    /// <summary>
    /// Send message with optional attachments to a single contact
    /// </summary>
    /// <param name="request">Message details including phone, message, and attachments</param>
    /// <returns>Send result</returns>
    /// <response code="200">Message sent successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Failed to send message</response>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<SendResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SendResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SendResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SendResultDto>>> SendMessage([FromBody] SendMessageRequest request)
    {
        _logger.LogInformation("Send message requested to {Phone}", request.Phone);

        if (string.IsNullOrEmpty(request.Phone))
        {
            return BadRequest(ApiResponse<SendResultDto>.ErrorResponse(
                "Phone number is required",
                "Invalid request"));
        }

        if (string.IsNullOrEmpty(request.Message) && (request.Attachments == null || !request.Attachments.Any()))
        {
            return BadRequest(ApiResponse<SendResultDto>.ErrorResponse(
                "Either message or attachments must be provided",
                "Invalid request"));
        }

        try
        {
            var result = await _whatsAppService.SendMessageAsync(request);

            if (result.Success)
            {
                return Ok(ApiResponse<SendResultDto>.SuccessResponse(
                    result,
                    $"Message sent to {request.Phone}"));
            }

            return StatusCode(500, ApiResponse<SendResultDto>.ErrorResponse(
                result.Error ?? "Unknown error",
                $"Failed to send message to {request.Phone}"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when sending message");
            return BadRequest(ApiResponse<SendResultDto>.ErrorResponse(
                ex.Message,
                "Cannot send message"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to {Phone}", request.Phone);
            return StatusCode(500, ApiResponse<SendResultDto>.ErrorResponse(
                ex.Message,
                "Failed to send message"));
        }
    }

    /// <summary>
    /// Send messages to multiple contacts with delays and breaks
    /// </summary>
    /// <param name="request">Bulk message request with contacts, message template, and settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk send results</returns>
    /// <response code="200">Bulk send completed</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Bulk send failed</response>
    [HttpPost("send-bulk")]
    [ProducesResponseType(typeof(ApiResponse<BulkResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BulkResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BulkResultDto>>> SendBulk(
        [FromBody] SendBulkRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bulk send requested for {Count} contacts", request.Contacts?.Count ?? 0);

        // Validate request
        if (request.Contacts == null || !request.Contacts.Any())
        {
            return BadRequest(ApiResponse<BulkResultDto>.ErrorResponse(
                "At least one contact is required",
                "Invalid request"));
        }

        if (string.IsNullOrEmpty(request.Message) && (request.Attachments == null || !request.Attachments.Any()))
        {
            return BadRequest(ApiResponse<BulkResultDto>.ErrorResponse(
                "Either message or attachments must be provided",
                "Invalid request"));
        }

        // Validate delay settings
        if (request.DelaySettings != null)
        {
            if (request.DelaySettings.MinDelaySeconds > request.DelaySettings.MaxDelaySeconds)
            {
                return BadRequest(ApiResponse<BulkResultDto>.ErrorResponse(
                    "MinDelaySeconds cannot be greater than MaxDelaySeconds",
                    "Invalid delay settings"));
            }
        }

        // Validate break settings
        if (request.BreakSettings != null)
        {
            if (request.BreakSettings.MinBreakMinutes > request.BreakSettings.MaxBreakMinutes)
            {
                return BadRequest(ApiResponse<BulkResultDto>.ErrorResponse(
                    "MinBreakMinutes cannot be greater than MaxBreakMinutes",
                    "Invalid break settings"));
            }
        }

        try
        {
            var result = await _whatsAppService.SendBulkAsync(request, cancellationToken);

            var message = $"Bulk send completed: {result.Sent} sent, {result.Failed} failed";

            return Ok(ApiResponse<BulkResultDto>.SuccessResponse(result, message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during bulk send");
            return BadRequest(ApiResponse<BulkResultDto>.ErrorResponse(
                ex.Message,
                "Cannot perform bulk send"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk send");
            return StatusCode(500, ApiResponse<BulkResultDto>.ErrorResponse(
                ex.Message,
                "Bulk send failed"));
        }
    }

    /// <summary>
    /// Close browser session
    /// </summary>
    /// <returns>Close status</returns>
    /// <response code="200">Browser closed successfully</response>
    [HttpPost("close")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> Close()
    {
        _logger.LogInformation("Close browser requested");

        try
        {
            await _whatsAppService.CloseAsync();
            return Ok(ApiResponse<string>.SuccessResponse(
                "Browser closed",
                "Browser session closed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing browser");
            return Ok(ApiResponse<string>.SuccessResponse(
                "Browser may already be closed",
                ex.Message));
        }
    }

    #region Bulk Operation Control (Start/Pause/Resume/Stop)

    /// <summary>
    /// Start bulk send operation (runs in background, use status/pause/resume/stop to control)
    /// </summary>
    /// <param name="request">Bulk message request with contacts, message template, and settings</param>
    /// <returns>Operation started response with operation ID</returns>
    /// <response code="200">Bulk operation started</response>
    /// <response code="400">Invalid request or operation already running</response>
    [HttpPost("bulk/start")]
    [ProducesResponseType(typeof(ApiResponse<BulkControlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkControlResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BulkControlResponse>>> StartBulk([FromBody] SendBulkRequest request)
    {
        _logger.LogInformation("Start bulk operation requested for {Count} contacts", request.Contacts?.Count ?? 0);

        // Validate request
        if (request.Contacts == null || !request.Contacts.Any())
        {
            return BadRequest(ApiResponse<BulkControlResponse>.ErrorResponse(
                "At least one contact is required",
                "Invalid request"));
        }

        if (string.IsNullOrEmpty(request.Message) && (request.Attachments == null || !request.Attachments.Any()))
        {
            return BadRequest(ApiResponse<BulkControlResponse>.ErrorResponse(
                "Either message or attachments must be provided",
                "Invalid request"));
        }

        var result = await _whatsAppService.StartBulkAsync(request);

        if (result.Success)
        {
            return Ok(ApiResponse<BulkControlResponse>.SuccessResponse(result, result.Message));
        }

        return BadRequest(ApiResponse<BulkControlResponse>.ErrorResponse(result.Message, "Cannot start bulk operation"));
    }

    /// <summary>
    /// Get current bulk operation status
    /// </summary>
    /// <returns>Current status including progress, sent/failed counts</returns>
    /// <response code="200">Returns current status</response>
    [HttpGet("bulk/status")]
    [ProducesResponseType(typeof(ApiResponse<BulkControlResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<BulkControlResponse>> GetBulkStatus()
    {
        var result = _whatsAppService.GetBulkStatus();
        return Ok(ApiResponse<BulkControlResponse>.SuccessResponse(result, result.Message));
    }

    /// <summary>
    /// Pause current bulk operation
    /// </summary>
    /// <returns>Pause result</returns>
    /// <response code="200">Operation paused successfully</response>
    /// <response code="400">No operation running to pause</response>
    [HttpPost("bulk/pause")]
    [ProducesResponseType(typeof(ApiResponse<BulkControlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkControlResponse>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<BulkControlResponse>> PauseBulk()
    {
        _logger.LogInformation("Pause bulk operation requested");

        var result = _whatsAppService.PauseBulk();

        if (result.Success)
        {
            return Ok(ApiResponse<BulkControlResponse>.SuccessResponse(result, result.Message));
        }

        return BadRequest(ApiResponse<BulkControlResponse>.ErrorResponse(result.Message, "Cannot pause"));
    }

    /// <summary>
    /// Resume paused bulk operation
    /// </summary>
    /// <returns>Resume result</returns>
    /// <response code="200">Operation resumed successfully</response>
    /// <response code="400">No paused operation to resume</response>
    [HttpPost("bulk/resume")]
    [ProducesResponseType(typeof(ApiResponse<BulkControlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkControlResponse>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<BulkControlResponse>> ResumeBulk()
    {
        _logger.LogInformation("Resume bulk operation requested");

        var result = _whatsAppService.ResumeBulk();

        if (result.Success)
        {
            return Ok(ApiResponse<BulkControlResponse>.SuccessResponse(result, result.Message));
        }

        return BadRequest(ApiResponse<BulkControlResponse>.ErrorResponse(result.Message, "Cannot resume"));
    }

    /// <summary>
    /// Stop current bulk operation
    /// </summary>
    /// <returns>Stop result with final counts</returns>
    /// <response code="200">Operation stopped successfully</response>
    /// <response code="400">No active operation to stop</response>
    [HttpPost("bulk/stop")]
    [ProducesResponseType(typeof(ApiResponse<BulkControlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkControlResponse>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<BulkControlResponse>> StopBulk()
    {
        _logger.LogInformation("Stop bulk operation requested");

        var result = _whatsAppService.StopBulk();

        if (result.Success)
        {
            return Ok(ApiResponse<BulkControlResponse>.SuccessResponse(result, result.Message));
        }

        return BadRequest(ApiResponse<BulkControlResponse>.ErrorResponse(result.Message, "Cannot stop"));
    }

    #endregion
}
