using System.ComponentModel.DataAnnotations;

namespace WhatsApp.Shared.Models
{
    /// <summary>
    /// Advanced timing settings with true randomization and decimal delays
    /// </summary>
    public class AdvancedTimingSettings
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// User ID this timing configuration belongs to
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Minimum delay between messages (in seconds, supports decimals)
        /// Example: 30.5 seconds
        /// </summary>
        [Range(1.0, 3600.0)]
        public double MinDelaySeconds { get; set; } = 30.0;

        /// <summary>
        /// Maximum delay between messages (in seconds, supports decimals)
        /// Example: 60.8 seconds
        /// </summary>
        [Range(1.0, 3600.0)]
        public double MaxDelaySeconds { get; set; } = 60.0;

        /// <summary>
        /// Enable random breaks after sending multiple messages
        /// </summary>
        public bool EnableRandomBreaks { get; set; } = true;

        /// <summary>
        /// Minimum number of messages before triggering a break
        /// Example: 13 messages
        /// </summary>
        [Range(1, 1000)]
        public int MinMessagesBeforeBreak { get; set; } = 13;

        /// <summary>
        /// Maximum number of messages before triggering a break
        /// Example: 20 messages
        /// </summary>
        [Range(1, 1000)]
        public int MaxMessagesBeforeBreak { get; set; } = 20;

        /// <summary>
        /// Minimum break duration (in minutes, supports decimals)
        /// Example: 4.2 minutes
        /// </summary>
        [Range(0.1, 120.0)]
        public double MinBreakMinutes { get; set; } = 4.0;

        /// <summary>
        /// Maximum break duration (in minutes, supports decimals)
        /// Example: 9.7 minutes
        /// </summary>
        [Range(0.1, 120.0)]
        public double MaxBreakMinutes { get; set; } = 9.0;

        /// <summary>
        /// Use strong randomization (non-integer, decimal values)
        /// When true: 32.7s, 54.3s, 43.9s
        /// When false: 32s, 54s, 43s
        /// </summary>
        public bool UseDecimalRandomization { get; set; } = true;

        /// <summary>
        /// Number of decimal places for randomization (1-3)
        /// 1 = 32.7s, 2 = 32.73s, 3 = 32.735s
        /// </summary>
        [Range(1, 3)]
        public int DecimalPrecision { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Request model for creating/updating timing settings
    /// </summary>
    public class AdvancedTimingSettingsRequest
    {
        /// <summary>
        /// Minimum delay between messages (seconds)
        /// </summary>
        [Range(1.0, 3600.0)]
        public double MinDelaySeconds { get; set; } = 30.0;

        /// <summary>
        /// Maximum delay between messages (seconds)
        /// </summary>
        [Range(1.0, 3600.0)]
        public double MaxDelaySeconds { get; set; } = 60.0;

        /// <summary>
        /// Enable random breaks
        /// </summary>
        public bool EnableRandomBreaks { get; set; } = true;

        /// <summary>
        /// Minimum messages before break
        /// </summary>
        [Range(1, 1000)]
        public int MinMessagesBeforeBreak { get; set; } = 13;

        /// <summary>
        /// Maximum messages before break
        /// </summary>
        [Range(1, 1000)]
        public int MaxMessagesBeforeBreak { get; set; } = 20;

        /// <summary>
        /// Minimum break duration (minutes)
        /// </summary>
        [Range(0.1, 120.0)]
        public double MinBreakMinutes { get; set; } = 4.0;

        /// <summary>
        /// Maximum break duration (minutes)
        /// </summary>
        [Range(0.1, 120.0)]
        public double MaxBreakMinutes { get; set; } = 9.0;

        /// <summary>
        /// Use decimal randomization
        /// </summary>
        public bool UseDecimalRandomization { get; set; } = true;

        /// <summary>
        /// Decimal precision (1-3)
        /// </summary>
        [Range(1, 3)]
        public int DecimalPrecision { get; set; } = 1;
    }

    /// <summary>
    /// Response model for timing settings
    /// </summary>
    public class AdvancedTimingSettingsResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public double MinDelaySeconds { get; set; }
        public double MaxDelaySeconds { get; set; }
        public bool EnableRandomBreaks { get; set; }
        public int MinMessagesBeforeBreak { get; set; }
        public int MaxMessagesBeforeBreak { get; set; }
        public double MinBreakMinutes { get; set; }
        public double MaxBreakMinutes { get; set; }
        public bool UseDecimalRandomization { get; set; }
        public int DecimalPrecision { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Calculated timing info for current message
    /// </summary>
    public class MessageTimingInfo
    {
        /// <summary>
        /// Delay before sending this message (in seconds)
        /// </summary>
        public double DelaySeconds { get; set; }

        /// <summary>
        /// Is this a break point?
        /// </summary>
        public bool IsBreakPoint { get; set; }

        /// <summary>
        /// Break duration if this is a break point (in minutes)
        /// </summary>
        public double? BreakDurationMinutes { get; set; }

        /// <summary>
        /// Messages sent since last break
        /// </summary>
        public int MessagesSinceLastBreak { get; set; }
    }
}
