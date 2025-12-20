using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppSender.API.Models;
using WhatsAppSender.API.Services;
using WhatsApp.Shared.Data;

namespace WhatsAppSender.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<WhatsAppController> _logger;
        private readonly SaaSDbContext _dbContext;

        public WhatsAppController(
            IWhatsAppService whatsAppService,
            IApiKeyService apiKeyService,
            ILogger<WhatsAppController> logger,
            SaaSDbContext dbContext)
        {
            _whatsAppService = whatsAppService;
            _apiKeyService = apiKeyService;
            _logger = logger;
            _dbContext = dbContext;
        }


        [HttpPost("send")]
        public async Task<IActionResult> SendMessages([FromBody] SendMessageRequest request)
        {
            try
            {
                if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
                {
                    return Unauthorized(new { error = "API key is required. Include X-API-Key header." });
                }

                var apiKeyValue = apiKeyHeader.FirstOrDefault();
                if (string.IsNullOrEmpty(apiKeyValue))
                {
                    return Unauthorized(new { error = "API key cannot be empty." });
                }

                var apiKey = await _apiKeyService.ValidateApiKeyAsync(apiKeyValue);
                if (apiKey == null)
                {
                    return Unauthorized(new { error = "Invalid or expired API key." });
                }

                if (request.Messages == null || !request.Messages.Any())
                {
                    return BadRequest(new { error = "At least one message is required." });
                }

                // Validate phone numbers and messages
                var uniquePhones = new HashSet<string>();
                var duplicatePhones = new List<string>();

                foreach (var message in request.Messages)
                {
                    if (string.IsNullOrEmpty(message.Phone))
                    {
                        return BadRequest(new { error = "Phone number is required for all messages." });
                    }

                    if (!message.Messages.Any() && !message.Files.Any())
                    {
                        return BadRequest(new { error = $"At least one text message or file is required for {message.Phone}." });
                    }

                    // Check for duplicate phone numbers
                    if (!uniquePhones.Add(message.Phone))
                    {
                        duplicatePhones.Add(message.Phone);
                    }
                }

                // Remove duplicates and warn user
                if (duplicatePhones.Any())
                {
                    _logger.LogWarning("Duplicate phone numbers detected and removed: {Phones}", string.Join(", ", duplicatePhones));
                    request.Messages = request.Messages
                        .GroupBy(m => m.Phone)
                        .Select(g => g.First()) // Take first occurrence of each phone
                        .ToList();
                }

                _logger.LogInformation("Processing message request for user: {UserEmail}, Recipients: {Count}",
                    apiKey.UserEmail, request.Messages.Count);

                // Determine timing settings
                TimingConfig timingConfig;
                bool useAutoMode = request.TimingConfig == null;

                if (useAutoMode)
                {
                    // Get timing from database (auto mode)
                    var activeTiming = await _dbContext.MessageTimingControls
                        .Where(mt => mt.IsActive && mt.SubscriptionPlanId == null)
                        .OrderByDescending(mt => mt.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (activeTiming != null)
                    {
                        timingConfig = new TimingConfig
                        {
                            MinDelaySeconds = activeTiming.MinDelaySeconds,
                            MaxDelaySeconds = activeTiming.MaxDelaySeconds,
                            UseStrongRandomization = true
                        };
                        _logger.LogInformation("Using auto timing mode: {Min}-{Max} seconds",
                            activeTiming.MinDelaySeconds, activeTiming.MaxDelaySeconds);
                    }
                    else
                    {
                        // Default fallback
                        timingConfig = new TimingConfig
                        {
                            MinDelaySeconds = 30,
                            MaxDelaySeconds = 60,
                            UseStrongRandomization = true
                        };
                        _logger.LogInformation("Using default timing: 30-60 seconds");
                    }
                }
                else
                {
                    // Use manual timing from request
                    timingConfig = request.TimingConfig;
                    _logger.LogInformation("Using manual timing mode: {Min}-{Max} seconds",
                        timingConfig.MinDelaySeconds, timingConfig.MaxDelaySeconds);
                }

                // Apply timing config to request
                request.TimingConfig = timingConfig;

                // If sending immediately (single batch), use original logic
                if (request.SendImmediately && request.Messages.Count == 1)
                {
                    var response = await _whatsAppService.SendMessagesAsync(request, apiKey: apiKey);
                    return response.Success ? Ok(response) : BadRequest(response);
                }

                // For multiple messages, send with timing delays
                var results = new List<MessageResult>();
                int successCount = 0;
                int failedCount = 0;
                var random = new Random();

                for (int i = 0; i < request.Messages.Count; i++)
                {
                    var message = request.Messages[i];
                    var singleRequest = new SendMessageRequest
                    {
                        Messages = new List<WhatsAppMessage> { message },
                        TimingConfig = timingConfig,
                        BrowserSettings = request.BrowserSettings,
                        SendImmediately = true
                    };

                    try
                    {
                        _logger.LogInformation("Sending message {Current}/{Total} to {Phone}",
                            i + 1, request.Messages.Count, message.Phone);

                        var result = await _whatsAppService.SendMessagesAsync(singleRequest, apiKey: apiKey);

                        results.Add(new MessageResult
                        {
                            Phone = message.Phone,
                            Success = result.Success,
                            Message = result.Message,
                            Error = result.Success ? null : string.Join(", ", result.Errors)
                        });

                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            failedCount++;
                        }

                        // Apply delay before next message (except for last message)
                        if (i < request.Messages.Count - 1)
                        {
                            int delaySeconds = random.Next(timingConfig.MinDelaySeconds, timingConfig.MaxDelaySeconds + 1);
                            int delayMs = delaySeconds * 1000;

                            _logger.LogInformation("Waiting {Delay} seconds before next message ({Next}/{Total})",
                                delaySeconds, i + 2, request.Messages.Count);

                            await Task.Delay(delayMs);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending message to {Phone}", message.Phone);
                        results.Add(new MessageResult
                        {
                            Phone = message.Phone,
                            Success = false,
                            Message = "Failed",
                            Error = ex.Message
                        });
                        failedCount++;
                    }
                }

                var overallResponse = new SendMessageWithTimingResponse
                {
                    Success = successCount > 0,
                    Message = $"Sent {successCount} out of {request.Messages.Count} messages",
                    TotalMessages = request.Messages.Count,
                    SuccessCount = successCount,
                    FailedCount = failedCount,
                    Results = results,
                    TimingMode = useAutoMode ? "auto" : "manual",
                    TimingSettings = new
                    {
                        min_delay_seconds = timingConfig.MinDelaySeconds,
                        max_delay_seconds = timingConfig.MaxDelaySeconds
                    }
                };

                return Ok(overallResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhatsApp message request");
                return StatusCode(500, new { error = "Internal server error occurred." });
            }
        }

        /// <summary>
        /// Get API usage statistics
        /// </summary>
        [HttpGet("usage")]
        public async Task<IActionResult> GetUsage()
        {
            try
            {
                if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
                {
                    return Unauthorized(new { error = "API key is required." });
                }

                var apiKeyValue = apiKeyHeader.FirstOrDefault();
                if (string.IsNullOrEmpty(apiKeyValue))
                {
                    return Unauthorized(new { error = "API key cannot be empty." });
                }

                var apiKey = await _apiKeyService.ValidateApiKeyAsync(apiKeyValue);
                if (apiKey == null)
                {
                    return Unauthorized(new { error = "Invalid or expired API key." });
                }

                var stats = await _apiKeyService.GetUsageStatsAsync(apiKeyValue);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage stats");
                return StatusCode(500, new { error = "Internal server error occurred." });
            }
        }

        /// <summary>
        /// Test API connection
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
                {
                    return Unauthorized(new { error = "API key is required." });
                }

                var apiKeyValue = apiKeyHeader.FirstOrDefault();
                if (string.IsNullOrEmpty(apiKeyValue))
                {
                    return Unauthorized(new { error = "API key cannot be empty." });
                }

                var apiKey = await _apiKeyService.ValidateApiKeyAsync(apiKeyValue);
                if (apiKey == null)
                {
                    return Unauthorized(new { error = "Invalid or expired API key." });
                }

                var isConnected = await _whatsAppService.TestConnectionAsync();

                return Ok(new
                {
                    success = true,
                    message = "API connection successful",
                    user = apiKey.UserEmail,
                    plan = apiKey.Subscription?.Name,
                    service_status = isConnected ? "Available" : "Unavailable"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                return StatusCode(500, new { error = "Internal server error occurred." });
            }
        }
    }
}