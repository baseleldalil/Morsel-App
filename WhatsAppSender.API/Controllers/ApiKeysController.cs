using Microsoft.AspNetCore.Mvc;
using WhatsAppSender.API.Services;

namespace WhatsAppSender.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiKeysController : ControllerBase
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<ApiKeysController> _logger;

        public ApiKeysController(IApiKeyService apiKeyService, ILogger<ApiKeysController> logger)
        {
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new API key for a user
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserEmail) || string.IsNullOrEmpty(request.Name))
                {
                    return BadRequest(new { error = "UserEmail and Name are required." });
                }

                if (request.SubscriptionId <= 0)
                {
                    return BadRequest(new { error = "Valid SubscriptionId is required." });
                }

                var apiKey = await _apiKeyService.CreateApiKeyAsync(request.UserEmail, request.SubscriptionId, request.Name);

                return Ok(new
                {
                    success = true,
                    message = "API key created successfully",
                    apiKey = apiKey.KeyValue,
                    name = apiKey.Name,
                    subscriptionId = apiKey.SubscriptionId,
                    expiresAt = apiKey.ExpiresAt,
                    createdAt = apiKey.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key for user: {UserEmail}", request.UserEmail);
                return StatusCode(500, new { error = "Internal server error occurred." });
            }
        }

        /// <summary>
        /// Get all API keys for a user
        /// </summary>
        [HttpGet("user/{userEmail}")]
        public async Task<IActionResult> GetUserApiKeys(string userEmail)
        {
            try
            {
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest(new { error = "UserEmail is required." });
                }

                var apiKeys = await _apiKeyService.GetUserApiKeysAsync(userEmail);

                var result = apiKeys.Select(static k => new
                {
                    id = k.Id,
                    name = k.Name,
                    keyPreview = k.KeyValue.Substring(0, 12) + "...", // Show only first 12 characters
                    subscriptionPlan = k.Subscription?.Name,
                    isActive = k.IsActive,
                    usageCount = k.UsageCount,
                    dailyUsageCount = k.DailyUsageCount,
                    lastUsedAt = k.LastUsedAt,
                    createdAt = k.CreatedAt,
                    expiresAt = k.ExpiresAt
                });

                return Ok(new
                {
                    success = true,
                    apiKeys = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API keys for user: {UserEmail}", userEmail);
                return StatusCode(500, new { error = "Internal server error occurred." });
            }
        }

        /// <summary>
        /// Revoke an API key
        /// </summary>
        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeApiKey([FromBody] RevokeApiKeyRequest request)
        {
            try
            {
                if (request.ApiKeyId <= 0 || string.IsNullOrEmpty(request.UserEmail))
                {
                    return BadRequest(new { error = "ApiKeyId and UserEmail are required." });
                }

                var success = await _apiKeyService.RevokeApiKeyAsync(request.ApiKeyId, request.UserEmail);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "API key revoked successfully"
                    });
                }
                else
                {
                    return NotFound(new { error = "API key not found or does not belong to the user." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key {ApiKeyId} for user: {UserEmail}",
                    request.ApiKeyId, request.UserEmail);
                return StatusCode(500, new { error = "Internal server error occurred." });
            }
        }
    }

    public class CreateApiKeyRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public int SubscriptionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RevokeApiKeyRequest
    {
        public int ApiKeyId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
    }
}