using Microsoft.AspNetCore.Mvc;
using WhatsAppSender.API.Models;
using WhatsAppSender.API.Services;

namespace WhatsAppSender.API.Controllers
{
    /// <summary>
    /// Controller for sending messages with configurable intervals in Auto or Manual mode
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MessageSendingController : ControllerBase
    {
        private readonly IMessageSendingService _messageSendingService;
        private readonly ILogger<MessageSendingController> _logger;

        public MessageSendingController(
            IMessageSendingService messageSendingService,
            ILogger<MessageSendingController> logger)
        {
            _messageSendingService = messageSendingService;
            _logger = logger;
        }

        /// <summary>
        /// Sends messages in Auto mode using configuration from appsettings.json
        /// </summary>
        /// <param name="phoneNumbers">List of phone numbers to send messages to</param>
        /// <param name="message">The message to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response with sending details</returns>
        /// <response code="200">Messages sent successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("send-auto")]
        [ProducesResponseType(typeof(SendMessagesWithModeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SendMessagesWithModeResponse>> SendInAutoMode(
            [FromBody] AutoModeSendRequest request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Received Auto mode send request for {Count} phone numbers",
                request.PhoneNumbers?.Count ?? 0);

            try
            {
                var sendRequest = new SendMessagesWithModeRequest
                {
                    Mode = SendingMode.Auto,
                    PhoneNumbers = request.PhoneNumbers ?? new List<string>(),
                    Message = request.Message ?? string.Empty
                };

                var response = await _messageSendingService.SendMessagesAsync(sendRequest, cancellationToken);

                if (!response.Success && response.Errors.Any())
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Auto mode send request");
                return StatusCode(500, new SendMessagesWithModeResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your request",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Sends messages in Manual mode using user-provided interval configuration
        /// </summary>
        /// <param name="request">Request containing phone numbers, message, and interval config</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response with sending details</returns>
        /// <response code="200">Messages sent successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("send-manual")]
        [ProducesResponseType(typeof(SendMessagesWithModeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SendMessagesWithModeResponse>> SendInManualMode(
            [FromBody] ManualModeSendRequest request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Received Manual mode send request for {Count} phone numbers with intervals: {Min}-{Max}s",
                request.PhoneNumbers?.Count ?? 0,
                request.MinIntervalSeconds,
                request.MaxIntervalSeconds);

            try
            {
                var sendRequest = new SendMessagesWithModeRequest
                {
                    Mode = SendingMode.Manual,
                    PhoneNumbers = request.PhoneNumbers ?? new List<string>(),
                    Message = request.Message ?? string.Empty,
                    ManualConfig = new SendingIntervalConfig
                    {
                        MinIntervalSeconds = request.MinIntervalSeconds,
                        MaxIntervalSeconds = request.MaxIntervalSeconds
                    }
                };

                var response = await _messageSendingService.SendMessagesAsync(sendRequest, cancellationToken);

                if (!response.Success && response.Errors.Any())
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Manual mode send request");
                return StatusCode(500, new SendMessagesWithModeResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your request",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Sends messages using a unified endpoint that supports both Auto and Manual modes
        /// </summary>
        /// <param name="request">Request containing mode, phone numbers, message, and optional interval config</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response with sending details</returns>
        /// <response code="200">Messages sent successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("send")]
        [ProducesResponseType(typeof(SendMessagesWithModeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SendMessagesWithModeResponse>> Send(
            [FromBody] SendMessagesWithModeRequest request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Received unified send request in {Mode} mode for {Count} phone numbers",
                request.Mode,
                request.PhoneNumbers?.Count ?? 0);

            try
            {
                var response = await _messageSendingService.SendMessagesAsync(request, cancellationToken);

                if (!response.Success && response.Errors.Any())
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unified send request");
                return StatusCode(500, new SendMessagesWithModeResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your request",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Gets the current Auto mode configuration from appsettings.json
        /// </summary>
        /// <returns>The current Auto mode configuration</returns>
        /// <response code="200">Configuration retrieved successfully</response>
        [HttpGet("auto-config")]
        [ProducesResponseType(typeof(SendingIntervalConfig), StatusCodes.Status200OK)]
        public ActionResult<SendingIntervalConfig> GetAutoConfig()
        {
            var config = _messageSendingService.GetAutoModeConfig();
            return Ok(config);
        }

        /// <summary>
        /// Validates an interval configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        /// <response code="200">Configuration is valid</response>
        /// <response code="400">Configuration is invalid</response>
        [HttpPost("validate-config")]
        [ProducesResponseType(typeof(ConfigValidationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ConfigValidationResponse), StatusCodes.Status400BadRequest)]
        public ActionResult<ConfigValidationResponse> ValidateConfig([FromBody] SendingIntervalConfig config)
        {
            var isValid = _messageSendingService.ValidateConfig(config, out var errorMessage);

            var response = new ConfigValidationResponse
            {
                IsValid = isValid,
                ErrorMessage = errorMessage
            };

            if (isValid)
            {
                response.Message = "Configuration is valid";
                return Ok(response);
            }

            return BadRequest(response);
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>Service status</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<object> HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "MessageSendingService",
                Timestamp = DateTime.UtcNow,
                AutoConfig = _messageSendingService.GetAutoModeConfig()
            });
        }
    }

    /// <summary>
    /// Request model for Auto mode sending
    /// </summary>
    public class AutoModeSendRequest
    {
        /// <summary>
        /// List of phone numbers to send messages to
        /// </summary>
        public List<string> PhoneNumbers { get; set; } = new();

        /// <summary>
        /// Message to send
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for Manual mode sending
    /// </summary>
    public class ManualModeSendRequest
    {
        /// <summary>
        /// List of phone numbers to send messages to
        /// </summary>
        public List<string> PhoneNumbers { get; set; } = new();

        /// <summary>
        /// Message to send
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Minimum interval between messages in seconds (must be >= 20)
        /// </summary>
        public int MinIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum interval between messages in seconds (must be >= MinIntervalSeconds)
        /// </summary>
        public int MaxIntervalSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Response model for configuration validation
    /// </summary>
    public class ConfigValidationResponse
    {
        /// <summary>
        /// Indicates if the configuration is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
