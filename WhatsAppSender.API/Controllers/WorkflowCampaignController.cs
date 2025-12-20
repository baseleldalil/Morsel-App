using Microsoft.AspNetCore.Mvc;
using WhatsApp.Shared.Data;
using WhatsAppSender.API.Models;
using WhatsAppSender.API.Services;

namespace WhatsAppSender.API.Controllers
{
    /// <summary>
    /// Controller for managing workflow campaign execution
    /// </summary>
    [ApiController]
    [Route("api/workflow-campaigns")]
    [Produces("application/json")]
    [Tags("Workflow Campaigns")]
    public class WorkflowCampaignController : ControllerBase
    {
        private readonly IWorkflowCampaignService _workflowService;
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<WorkflowCampaignController> _logger;

        public WorkflowCampaignController(
            IWorkflowCampaignService workflowService,
            IApiKeyService apiKeyService,
            ILogger<WorkflowCampaignController> logger)
        {
            _workflowService = workflowService;
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        /// <summary>
        /// Start a workflow campaign
        /// </summary>
        /// <param name="request">Campaign ID, browser type, and timing mode</param>
        /// <returns>Start result with timing settings</returns>
        /// <remarks>
        /// Starts processing a campaign workflow with the specified settings.
        ///
        /// **Browser options:** chrome, firefox
        ///
        /// **Timing modes:**
        /// - **manual**: Uses user's AdvancedTimingSettings from database
        /// - **auto**: Uses system admin's AdvancedTimingSettings
        ///
        /// **Process flow:**
        /// 1. Gets all workflow entries for campaign
        /// 2. Loads timing settings based on mode
        /// 3. Loops through contacts with randomized delays
        /// 4. Applies message variable randomization ({a|b|c} format)
        /// 5. Handles breaks after X messages
        /// 6. Updates contact/workflow status on send
        ///
        /// Sample request:
        ///
        ///     POST /api/workflow-campaigns/start
        ///     {
        ///         "campaignId": 123,
        ///         "browser": "chrome",
        ///         "timingMode": "manual"
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Campaign started successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("start")]
        [ProducesResponseType(typeof(StartWorkflowCampaignResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> StartWorkflowCampaign([FromBody] StartWorkflowCampaignRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                _logger.LogInformation("Starting workflow campaign {CampaignId} for user {UserId} with browser {Browser} and timing {TimingMode}",
                    request.CampaignId, userId, request.Browser, request.TimingMode);

                var result = await _workflowService.StartCampaignAsync(
                    request.CampaignId,
                    userId,
                    request.Browser,
                    request.TimingMode);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting workflow campaign {CampaignId}", request.CampaignId);
                return StatusCode(500, new { error = "Internal server error while starting campaign" });
            }
        }

        /// <summary>
        /// Get workflow campaign progress
        /// </summary>
        /// <param name="campaignId">Campaign ID</param>
        /// <returns>Progress percentage and contact statistics</returns>
        /// <remarks>
        /// Returns detailed progress information including:
        /// - Progress percentage
        /// - Contact statistics by status (pending, processing, sent, delivered, failed, bounced)
        /// - Success rate
        /// - Current processing info (current contact, messages since break)
        /// - Break status (if on break, remaining time)
        /// - Estimated remaining time
        ///
        /// Sample request:
        ///
        ///     GET /api/workflow-campaigns/123/progress
        ///
        /// </remarks>
        /// <response code="200">Progress retrieved successfully</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{campaignId}/progress")]
        [ProducesResponseType(typeof(WorkflowCampaignProgressResponse), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetWorkflowCampaignProgress(int campaignId)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                var result = await _workflowService.GetProgressAsync(campaignId, userId);

                if (result.Status == "NotFound")
                {
                    return NotFound(new { error = "Campaign not found or access denied" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress for campaign {CampaignId}", campaignId);
                return StatusCode(500, new { error = "Internal server error while getting campaign progress" });
            }
        }

        /// <summary>
        /// Stop a workflow campaign
        /// </summary>
        /// <param name="request">Campaign ID to stop</param>
        /// <returns>Stop result with remaining contact count</returns>
        /// <remarks>
        /// Stops a running campaign:
        /// - Changes workflow status to Stopped
        /// - Closes the browser (Chrome/Firefox)
        /// - Stops all background processing
        /// - Campaign cannot be resumed after stopping
        ///
        /// Sample request:
        ///
        ///     POST /api/workflow-campaigns/stop
        ///     {
        ///         "campaignId": 123
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Campaign stopped successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("stop")]
        [ProducesResponseType(typeof(StopWorkflowCampaignResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> StopWorkflowCampaign([FromBody] StopWorkflowCampaignRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                _logger.LogInformation("Stopping workflow campaign {CampaignId} for user {UserId}",
                    request.CampaignId, userId);

                var result = await _workflowService.StopCampaignAsync(request.CampaignId, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping workflow campaign {CampaignId}", request.CampaignId);
                return StatusCode(500, new { error = "Internal server error while stopping campaign" });
            }
        }

        /// <summary>
        /// Pause a workflow campaign
        /// </summary>
        /// <param name="request">Campaign ID to pause</param>
        /// <returns>Pause result with processed/remaining counts</returns>
        /// <remarks>
        /// Pauses a running campaign:
        /// - Changes workflow status to Paused
        /// - Pauses background processing (can be resumed)
        /// - Browser remains open
        ///
        /// Sample request:
        ///
        ///     POST /api/workflow-campaigns/pause
        ///     {
        ///         "campaignId": 123
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Campaign paused successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("pause")]
        [ProducesResponseType(typeof(PauseWorkflowCampaignResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> PauseWorkflowCampaign([FromBody] PauseWorkflowCampaignRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                _logger.LogInformation("Pausing workflow campaign {CampaignId} for user {UserId}",
                    request.CampaignId, userId);

                var result = await _workflowService.PauseCampaignAsync(request.CampaignId, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing workflow campaign {CampaignId}", request.CampaignId);
                return StatusCode(500, new { error = "Internal server error while pausing campaign" });
            }
        }

        /// <summary>
        /// Resume a paused workflow campaign
        /// </summary>
        /// <param name="request">Campaign ID and browser to use</param>
        /// <returns>Resume result with remaining contact count</returns>
        /// <remarks>
        /// Resumes a paused campaign:
        /// - Changes workflow status to Running
        /// - Resumes processing from where it left off
        /// - Loops through remaining contacts with pending/new status
        /// - Continues sending messages to WhatsApp
        ///
        /// Sample request:
        ///
        ///     POST /api/workflow-campaigns/resume
        ///     {
        ///         "campaignId": 123,
        ///         "browser": "chrome"
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Campaign resumed successfully</response>
        /// <response code="400">Invalid request or campaign not paused</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("resume")]
        [ProducesResponseType(typeof(ResumeWorkflowCampaignResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> ResumeWorkflowCampaign([FromBody] ResumeWorkflowCampaignRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                _logger.LogInformation("Resuming workflow campaign {CampaignId} for user {UserId} with browser {Browser}",
                    request.CampaignId, userId, request.Browser);

                var result = await _workflowService.ResumeCampaignAsync(request.CampaignId, userId, request.Browser);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming workflow campaign {CampaignId}", request.CampaignId);
                return StatusCode(500, new { error = "Internal server error while resuming campaign" });
            }
        }
    }
}
