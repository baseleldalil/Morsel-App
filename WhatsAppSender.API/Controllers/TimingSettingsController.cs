using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppSender.API.Services;

namespace WhatsAppSender.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimingSettingsController : ControllerBase
    {
        private readonly SaaSDbContext _context;
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<TimingSettingsController> _logger;

        public TimingSettingsController(
            SaaSDbContext context,
            IApiKeyService apiKeyService,
            ILogger<TimingSettingsController> logger)
        {
            _context = context;
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        /// <summary>
        /// Get timing settings for current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTimingSettings()
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

                // Get or create default settings for this user
                var settings = await _context.AdvancedTimingSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    // Create default settings
                    settings = new AdvancedTimingSettings
                    {
                        UserId = userId,
                        MinDelaySeconds = 30.0,
                        MaxDelaySeconds = 60.0,
                        EnableRandomBreaks = true,
                        MinMessagesBeforeBreak = 13,
                        MaxMessagesBeforeBreak = 20,
                        MinBreakMinutes = 4.0,
                        MaxBreakMinutes = 9.0,
                        UseDecimalRandomization = true,
                        DecimalPrecision = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.AdvancedTimingSettings.Add(settings);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created default timing settings for user: {UserId}", userId);
                }

                var response = new AdvancedTimingSettingsResponse
                {
                    Id = settings.Id,
                    UserId = settings.UserId,
                    MinDelaySeconds = settings.MinDelaySeconds,
                    MaxDelaySeconds = settings.MaxDelaySeconds,
                    EnableRandomBreaks = settings.EnableRandomBreaks,
                    MinMessagesBeforeBreak = settings.MinMessagesBeforeBreak,
                    MaxMessagesBeforeBreak = settings.MaxMessagesBeforeBreak,
                    MinBreakMinutes = settings.MinBreakMinutes,
                    MaxBreakMinutes = settings.MaxBreakMinutes,
                    UseDecimalRandomization = settings.UseDecimalRandomization,
                    DecimalPrecision = settings.DecimalPrecision,
                    CreatedAt = settings.CreatedAt,
                    UpdatedAt = settings.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving timing settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Update timing settings for current user
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateTimingSettings([FromBody] AdvancedTimingSettingsRequest request)
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

                // Validate request
                if (request.MinDelaySeconds > request.MaxDelaySeconds)
                {
                    return BadRequest(new { error = "MinDelaySeconds must be less than or equal to MaxDelaySeconds" });
                }

                if (request.MinBreakMinutes > request.MaxBreakMinutes)
                {
                    return BadRequest(new { error = "MinBreakMinutes must be less than or equal to MaxBreakMinutes" });
                }

                if (request.MinMessagesBeforeBreak > request.MaxMessagesBeforeBreak)
                {
                    return BadRequest(new { error = "MinMessagesBeforeBreak must be less than or equal to MaxMessagesBeforeBreak" });
                }

                // Get or create settings
                var settings = await _context.AdvancedTimingSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    settings = new AdvancedTimingSettings
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.AdvancedTimingSettings.Add(settings);
                }

                // Update settings
                settings.MinDelaySeconds = request.MinDelaySeconds;
                settings.MaxDelaySeconds = request.MaxDelaySeconds;
                settings.EnableRandomBreaks = request.EnableRandomBreaks;
                settings.MinMessagesBeforeBreak = request.MinMessagesBeforeBreak;
                settings.MaxMessagesBeforeBreak = request.MaxMessagesBeforeBreak;
                settings.MinBreakMinutes = request.MinBreakMinutes;
                settings.MaxBreakMinutes = request.MaxBreakMinutes;
                settings.UseDecimalRandomization = request.UseDecimalRandomization;
                settings.DecimalPrecision = request.DecimalPrecision;
                settings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated timing settings for user {UserId}: Delays={Min}-{Max}s, Breaks={EnableBreaks}, Messages={MinMsg}-{MaxMsg}",
                    userId,
                    settings.MinDelaySeconds,
                    settings.MaxDelaySeconds,
                    settings.EnableRandomBreaks,
                    settings.MinMessagesBeforeBreak,
                    settings.MaxMessagesBeforeBreak
                );

                var response = new AdvancedTimingSettingsResponse
                {
                    Id = settings.Id,
                    UserId = settings.UserId,
                    MinDelaySeconds = settings.MinDelaySeconds,
                    MaxDelaySeconds = settings.MaxDelaySeconds,
                    EnableRandomBreaks = settings.EnableRandomBreaks,
                    MinMessagesBeforeBreak = settings.MinMessagesBeforeBreak,
                    MaxMessagesBeforeBreak = settings.MaxMessagesBeforeBreak,
                    MinBreakMinutes = settings.MinBreakMinutes,
                    MaxBreakMinutes = settings.MaxBreakMinutes,
                    UseDecimalRandomization = settings.UseDecimalRandomization,
                    DecimalPrecision = settings.DecimalPrecision,
                    CreatedAt = settings.CreatedAt,
                    UpdatedAt = settings.UpdatedAt
                };

                return Ok(new
                {
                    message = "Timing settings updated successfully",
                    settings = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timing settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Reset timing settings to defaults
        /// </summary>
        [HttpPost("reset")]
        public async Task<IActionResult> ResetToDefaults()
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

                var settings = await _context.AdvancedTimingSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings != null)
                {
                    settings.MinDelaySeconds = 30.0;
                    settings.MaxDelaySeconds = 60.0;
                    settings.EnableRandomBreaks = true;
                    settings.MinMessagesBeforeBreak = 13;
                    settings.MaxMessagesBeforeBreak = 20;
                    settings.MinBreakMinutes = 4.0;
                    settings.MaxBreakMinutes = 9.0;
                    settings.UseDecimalRandomization = true;
                    settings.DecimalPrecision = 1;
                    settings.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Reset timing settings to defaults for user: {UserId}", userId);

                return Ok(new { message = "Timing settings reset to defaults" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting timing settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
