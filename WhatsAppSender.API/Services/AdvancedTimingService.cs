using WhatsApp.Shared.Models;

namespace WhatsAppSender.API.Services
{
    /// <summary>
    /// Service for advanced timing with true randomization and decimal delays
    /// </summary>
    public interface IAdvancedTimingService
    {
        /// <summary>
        /// Calculate timing for the next message
        /// </summary>
        MessageTimingInfo CalculateNextMessageTiming(
            AdvancedTimingSettings settings,
            int messagesSinceLastBreak);

        /// <summary>
        /// Generate random delay within range (with decimal precision)
        /// </summary>
        double GenerateRandomDelay(
            double minSeconds,
            double maxSeconds,
            bool useDecimalRandomization,
            int decimalPrecision);

        /// <summary>
        /// Generate random break duration (in minutes)
        /// </summary>
        double GenerateRandomBreakDuration(
            double minMinutes,
            double maxMinutes,
            bool useDecimalRandomization,
            int decimalPrecision);

        /// <summary>
        /// Determine if a break should occur at this point
        /// </summary>
        bool ShouldTriggerBreak(
            AdvancedTimingSettings settings,
            int messagesSinceLastBreak);
    }

    public class AdvancedTimingService : IAdvancedTimingService
    {
        private readonly ILogger<AdvancedTimingService> _logger;
        private readonly Random _random;
        private readonly Random _cryptoRandom; // For more unpredictable randomness

        public AdvancedTimingService(ILogger<AdvancedTimingService> logger)
        {
            _logger = logger;

            // Use multiple random sources for true unpredictability
            _random = new Random(Guid.NewGuid().GetHashCode());
            _cryptoRandom = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));
        }

        /// <summary>
        /// Calculate timing for the next message with full randomization
        /// </summary>
        public MessageTimingInfo CalculateNextMessageTiming(
            AdvancedTimingSettings settings,
            int messagesSinceLastBreak)
        {
            var timingInfo = new MessageTimingInfo
            {
                MessagesSinceLastBreak = messagesSinceLastBreak
            };

            // Generate random delay for this message
            timingInfo.DelaySeconds = GenerateRandomDelay(
                settings.MinDelaySeconds,
                settings.MaxDelaySeconds,
                settings.UseDecimalRandomization,
                settings.DecimalPrecision
            );

            // Check if we should trigger a break
            if (settings.EnableRandomBreaks)
            {
                timingInfo.IsBreakPoint = ShouldTriggerBreak(settings, messagesSinceLastBreak);

                if (timingInfo.IsBreakPoint)
                {
                    timingInfo.BreakDurationMinutes = GenerateRandomBreakDuration(
                        settings.MinBreakMinutes,
                        settings.MaxBreakMinutes,
                        settings.UseDecimalRandomization,
                        settings.DecimalPrecision
                    );

                    _logger.LogInformation(
                        "🛑 Break point triggered after {Messages} messages. Break duration: {Duration:F1} minutes",
                        messagesSinceLastBreak,
                        timingInfo.BreakDurationMinutes
                    );
                }
            }

            _logger.LogDebug(
                "⏱️ Message timing calculated: Delay={Delay:F1}s, IsBreak={IsBreak}, MessageCount={Count}",
                timingInfo.DelaySeconds,
                timingInfo.IsBreakPoint,
                messagesSinceLastBreak
            );

            return timingInfo;
        }

        /// <summary>
        /// Generate truly random delay with decimal precision
        /// Uses multiple randomization techniques for unpredictability
        /// </summary>
        public double GenerateRandomDelay(
            double minSeconds,
            double maxSeconds,
            bool useDecimalRandomization,
            int decimalPrecision)
        {
            if (minSeconds > maxSeconds)
            {
                (minSeconds, maxSeconds) = (maxSeconds, minSeconds);
            }

            double delay;

            if (useDecimalRandomization)
            {
                // Generate truly random decimal value
                // Mix two random sources for better unpredictability
                var randomFactor1 = _random.NextDouble();
                var randomFactor2 = _cryptoRandom.NextDouble();

                // Combine both random factors for true unpredictability
                var combinedFactor = (randomFactor1 + randomFactor2) / 2.0;

                // Calculate delay within range
                delay = minSeconds + (combinedFactor * (maxSeconds - minSeconds));

                // Round to specified decimal precision
                delay = Math.Round(delay, decimalPrecision);
            }
            else
            {
                // Integer randomization (no decimals)
                delay = _random.Next((int)minSeconds, (int)maxSeconds + 1);
            }

            // Ensure delay is within bounds
            delay = Math.Max(minSeconds, Math.Min(delay, maxSeconds));

            _logger.LogDebug(
                "🎲 Generated random delay: {Delay:F3}s (Range: {Min}-{Max}s, Decimal: {UseDecimal})",
                delay, minSeconds, maxSeconds, useDecimalRandomization
            );

            return delay;
        }

        /// <summary>
        /// Generate random break duration in minutes
        /// </summary>
        public double GenerateRandomBreakDuration(
            double minMinutes,
            double maxMinutes,
            bool useDecimalRandomization,
            int decimalPrecision)
        {
            if (minMinutes > maxMinutes)
            {
                (minMinutes, maxMinutes) = (maxMinutes, minMinutes);
            }

            double duration;

            if (useDecimalRandomization)
            {
                // Mix two random sources
                var randomFactor1 = _random.NextDouble();
                var randomFactor2 = _cryptoRandom.NextDouble();
                var combinedFactor = (randomFactor1 + randomFactor2) / 2.0;

                duration = minMinutes + (combinedFactor * (maxMinutes - minMinutes));
                duration = Math.Round(duration, decimalPrecision);
            }
            else
            {
                duration = _random.Next((int)minMinutes, (int)maxMinutes + 1);
            }

            duration = Math.Max(minMinutes, Math.Min(duration, maxMinutes));

            _logger.LogDebug(
                "⏸️ Generated break duration: {Duration:F3} minutes (Range: {Min}-{Max} minutes)",
                duration, minMinutes, maxMinutes
            );

            return duration;
        }

        /// <summary>
        /// Determine if break should be triggered (randomly)
        /// </summary>
        public bool ShouldTriggerBreak(
            AdvancedTimingSettings settings,
            int messagesSinceLastBreak)
        {
            if (!settings.EnableRandomBreaks)
            {
                return false;
            }

            // Generate random threshold for this break cycle
            // This ensures break happens at different message counts each time
            var breakThreshold = _random.Next(
                settings.MinMessagesBeforeBreak,
                settings.MaxMessagesBeforeBreak + 1
            );

            var shouldBreak = messagesSinceLastBreak >= breakThreshold;

            if (shouldBreak)
            {
                _logger.LogInformation(
                    "✅ Break threshold reached: {Current} >= {Threshold} (Range: {Min}-{Max})",
                    messagesSinceLastBreak,
                    breakThreshold,
                    settings.MinMessagesBeforeBreak,
                    settings.MaxMessagesBeforeBreak
                );
            }

            return shouldBreak;
        }
    }
}
