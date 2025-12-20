using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppSender.API.Models;

namespace WhatsAppSender.API.Services
{
    public interface ITimingService
    {
        Task<(int minDelay, int maxDelay)> GetMessageDelayAsync(int? subscriptionPlanId = null);
        Task<(int beforeUpload, int uploadTime, int afterUpload)> GetVideoDelayAsync(int? subscriptionPlanId = null);
        Task<RandomDelayRule?> GetApplicableDelayRuleAsync(int messageCount, int? subscriptionPlanId = null);
        int CalculateDelay(int minSeconds, int maxSeconds, bool useStrongRandomization = true);
        int CalculateVideoDelay(int minSeconds, int maxSeconds, bool useStrongRandomization = true);
        int? CalculateMessageBasedPause(int messagesSent, Dictionary<int, int>? pauseRules, bool useStrongRandomization = true);
    }

    public class TimingService : ITimingService
    {
        private readonly ILogger<TimingService> _logger;
        private readonly SaaSDbContext _context;
        private readonly Random _random;

        public TimingService(ILogger<TimingService> logger, SaaSDbContext context)
        {
            _logger = logger;
            _context = context;
            // Use Guid-based seed for better randomization across instances
            _random = new Random(Guid.NewGuid().GetHashCode());
        }

        /// <summary>
        /// Get message delay configuration from database (Admin Panel Timing Control)
        /// </summary>
        public async Task<(int minDelay, int maxDelay)> GetMessageDelayAsync(int? subscriptionPlanId = null)
        {
            try
            {
                // Try to get subscription-specific timing first
                if (subscriptionPlanId.HasValue)
                {
                    var subscriptionTiming = await _context.MessageTimingControls
                        .Where(m => m.IsActive && m.SubscriptionPlanId == subscriptionPlanId)
                        .FirstOrDefaultAsync();

                    if (subscriptionTiming != null)
                    {
                        _logger.LogDebug("Using subscription-specific timing: {Min}-{Max}s",
                            subscriptionTiming.MinDelaySeconds, subscriptionTiming.MaxDelaySeconds);
                        return (subscriptionTiming.MinDelaySeconds, subscriptionTiming.MaxDelaySeconds);
                    }
                }

                // Fallback to global default timing
                var globalTiming = await _context.MessageTimingControls
                    .Where(m => m.IsActive && m.SubscriptionPlanId == null)
                    .FirstOrDefaultAsync();

                if (globalTiming != null)
                {
                    _logger.LogDebug("Using global timing: {Min}-{Max}s",
                        globalTiming.MinDelaySeconds, globalTiming.MaxDelaySeconds);
                    return (globalTiming.MinDelaySeconds, globalTiming.MaxDelaySeconds);
                }

                // Final fallback: default values if nothing configured
                _logger.LogWarning("No timing configuration found in database. Using default: 1-3s");
                return (1, 3);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching message timing from database. Using default: 1-3s");
                return (1, 3);
            }
        }

        /// <summary>
        /// Get video timing configuration from database (Admin Panel Timing Control)
        /// </summary>
        public async Task<(int beforeUpload, int uploadTime, int afterUpload)> GetVideoDelayAsync(int? subscriptionPlanId = null)
        {
            try
            {
                // Try subscription-specific timing first
                if (subscriptionPlanId.HasValue)
                {
                    var subscriptionTiming = await _context.VideoTimingControls
                        .Where(v => v.IsActive && v.SubscriptionPlanId == subscriptionPlanId)
                        .FirstOrDefaultAsync();

                    if (subscriptionTiming != null)
                    {
                        var beforeUpload = CalculateDelay(subscriptionTiming.MinDelayBeforeUploadSeconds,
                            subscriptionTiming.MaxDelayBeforeUploadSeconds, true);
                        var uploadTime = CalculateDelay(subscriptionTiming.MinUploadTimeSeconds,
                            subscriptionTiming.MaxUploadTimeSeconds, true);
                        var afterUpload = CalculateDelay(subscriptionTiming.MinDelayAfterUploadSeconds,
                            subscriptionTiming.MaxDelayAfterUploadSeconds, true);

                        _logger.LogDebug("Using subscription-specific video timing: before={Before}s, upload={Upload}s, after={After}s",
                            beforeUpload, uploadTime, afterUpload);
                        return (beforeUpload, uploadTime, afterUpload);
                    }
                }

                // Fallback to global default
                var globalTiming = await _context.VideoTimingControls
                    .Where(v => v.IsActive && v.SubscriptionPlanId == null)
                    .FirstOrDefaultAsync();

                if (globalTiming != null)
                {
                    var beforeUpload = CalculateDelay(globalTiming.MinDelayBeforeUploadSeconds,
                        globalTiming.MaxDelayBeforeUploadSeconds, true);
                    var uploadTime = CalculateDelay(globalTiming.MinUploadTimeSeconds,
                        globalTiming.MaxUploadTimeSeconds, true);
                    var afterUpload = CalculateDelay(globalTiming.MinDelayAfterUploadSeconds,
                        globalTiming.MaxDelayAfterUploadSeconds, true);

                    _logger.LogDebug("Using global video timing: before={Before}s, upload={Upload}s, after={After}s",
                        beforeUpload, uploadTime, afterUpload);
                    return (beforeUpload, uploadTime, afterUpload);
                }

                // Final fallback
                _logger.LogWarning("No video timing configuration found. Using defaults: 5s, 15s, 5s");
                return (5, 15, 5);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching video timing from database. Using defaults");
                return (5, 15, 5);
            }
        }

        /// <summary>
        /// Get applicable random delay rule from database (Admin Panel Timing Control)
        /// </summary>
        public async Task<RandomDelayRule?> GetApplicableDelayRuleAsync(int messageCount, int? subscriptionPlanId = null)
        {
            try
            {
                var query = _context.RandomDelayRules.Where(r => r.IsActive);

                // Try subscription-specific rules first
                if (subscriptionPlanId.HasValue)
                {
                    var subscriptionRules = await query
                        .Where(r => r.SubscriptionPlanId == subscriptionPlanId)
                        .OrderBy(r => r.Priority)
                        .ToListAsync();

                    var matchingRule = subscriptionRules.FirstOrDefault(r =>
                        messageCount >= r.AfterMessageCount &&
                        messageCount % r.AfterMessageCount == 0);

                    if (matchingRule != null)
                    {
                        _logger.LogInformation("Found subscription-specific random delay rule: {RuleName} after {Count} messages",
                            matchingRule.Name, messageCount);
                        return matchingRule;
                    }
                }

                // Fallback to global rules
                var globalRules = await query
                    .Where(r => r.SubscriptionPlanId == null)
                    .OrderBy(r => r.Priority)
                    .ToListAsync();

                var globalMatch = globalRules.FirstOrDefault(r =>
                    messageCount >= r.AfterMessageCount &&
                    messageCount % r.AfterMessageCount == 0);

                if (globalMatch != null)
                {
                    _logger.LogInformation("Found global random delay rule: {RuleName} after {Count} messages",
                        globalMatch.Name, messageCount);
                }

                return globalMatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching random delay rules from database");
                return null;
            }
        }

        /// <summary>
        /// Calculate delay for regular messages with strong randomization
        /// </summary>
        public int CalculateDelay(int minSeconds, int maxSeconds, bool useStrongRandomization = true)
        {
            if (minSeconds >= maxSeconds)
                return minSeconds;

            if (useStrongRandomization)
            {
                // Strong randomization: Use "broken" numbers, not clean integers
                // Add micro-variations to make delays appear more human
                double baseDelay = _random.Next(minSeconds, maxSeconds + 1);

                // Add fractional seconds (0.1 to 0.9 seconds)
                double microVariation = _random.NextDouble() * 0.9 + 0.1;

                // Add occasional extra jitter (-2 to +3 seconds)
                int jitter = _random.Next(-2, 4);

                int finalDelay = (int)(baseDelay + microVariation + jitter);

                // Ensure delay is within reasonable bounds (at least 1 second)
                finalDelay = Math.Max(1, finalDelay);

                _logger.LogDebug("Strong randomization delay: {Delay} seconds (range: {Min}-{Max})",
                    finalDelay, minSeconds, maxSeconds);

                return finalDelay;
            }
            else
            {
                // Simple randomization
                int delay = _random.Next(minSeconds, maxSeconds + 1);
                _logger.LogDebug("Simple randomization delay: {Delay} seconds", delay);
                return delay;
            }
        }

        /// <summary>
        /// Calculate delay for video messages (typically longer)
        /// </summary>
        public int CalculateVideoDelay(int minSeconds, int maxSeconds, bool useStrongRandomization = true)
        {
            if (minSeconds >= maxSeconds)
                return minSeconds;

            if (useStrongRandomization)
            {
                // Videos need more time for upload/processing
                double baseDelay = _random.Next(minSeconds, maxSeconds + 1);

                // Add larger micro-variations for videos (0.5 to 2 seconds)
                double microVariation = _random.NextDouble() * 1.5 + 0.5;

                // Add occasional extra processing time (0 to +5 seconds)
                int processingTime = _random.Next(0, 6);

                int finalDelay = (int)(baseDelay + microVariation + processingTime);

                // Ensure minimum delay for videos (at least 10 seconds)
                finalDelay = Math.Max(10, finalDelay);

                _logger.LogDebug("Video delay with strong randomization: {Delay} seconds (range: {Min}-{Max})",
                    finalDelay, minSeconds, maxSeconds);

                return finalDelay;
            }
            else
            {
                int delay = _random.Next(minSeconds, maxSeconds + 1);
                _logger.LogDebug("Video delay with simple randomization: {Delay} seconds", delay);
                return delay;
            }
        }

        /// <summary>
        /// Calculate message-based pause duration
        /// Example: After 14 messages, pause for 4 minutes with randomization
        /// </summary>
        public int? CalculateMessageBasedPause(int messagesSent, Dictionary<int, int>? pauseRules, bool useStrongRandomization = true)
        {
            if (pauseRules == null || pauseRules.Count == 0)
                return null;

            // Check if current message count matches any pause rule
            if (pauseRules.TryGetValue(messagesSent, out int pauseSeconds))
            {
                if (useStrongRandomization)
                {
                    // Add random variation to pause duration (-10% to +15%)
                    double variationPercent = _random.NextDouble() * 0.25 - 0.10; // -10% to +15%
                    int variation = (int)(pauseSeconds * variationPercent);

                    // Add micro jitter (0 to 30 seconds)
                    int microJitter = _random.Next(0, 31);

                    int finalPause = pauseSeconds + variation + microJitter;

                    // Ensure minimum pause (at least 30 seconds)
                    finalPause = Math.Max(30, finalPause);

                    _logger.LogInformation("Message-based pause triggered after {Count} messages. Pausing for {Duration} seconds (base: {Base}s)",
                        messagesSent, finalPause, pauseSeconds);

                    return finalPause;
                }
                else
                {
                    _logger.LogInformation("Message-based pause triggered after {Count} messages. Pausing for {Duration} seconds",
                        messagesSent, pauseSeconds);
                    return pauseSeconds;
                }
            }

            return null;
        }
    }
}
