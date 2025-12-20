using System.ComponentModel.DataAnnotations;

namespace WhatsAppSender.API.Models
{
    /// <summary>
    /// Configuration for message timing and delays
    /// DEPRECATED: This model is no longer used. Timing is now controlled from the Admin Panel.
    /// See: https://localhost:50645/TimingControl
    /// Timing is automatically fetched from the database based on the user's subscription plan.
    /// </summary>
    [Obsolete("Timing is now controlled from the Admin Panel. This configuration is ignored.")]
    public class TimingConfiguration
    {
        /// <summary>
        /// Minimum delay between messages in seconds
        /// </summary>
        [Range(0, int.MaxValue)]
        public int MinDelaySeconds { get; set; } = 5;

        /// <summary>
        /// Maximum delay between messages in seconds
        /// </summary>
        [Range(0, int.MaxValue)]
        public int MaxDelaySeconds { get; set; } = 15;

        /// <summary>
        /// Enable message-based pause logic
        /// Example: After 14 messages, pause for 4 minutes
        /// </summary>
        public bool EnableMessageBasedPauses { get; set; } = false;

        /// <summary>
        /// Message-based pause rules
        /// Key: Message count (after how many messages)
        /// Value: Pause duration in seconds
        /// Example: {14: 240} means after 14 messages, pause for 4 minutes (240 seconds)
        /// </summary>
        public Dictionary<int, int> MessagePauseRules { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Use strong randomization for delays
        /// When true, uses imperfect/broken numbers instead of clean integers
        /// </summary>
        public bool UseStrongRandomization { get; set; } = true;

        /// <summary>
        /// Minimum delay for video messages in seconds
        /// Videos require more time for upload/processing
        /// </summary>
        [Range(0, int.MaxValue)]
        public int VideoMinDelaySeconds { get; set; } = 20;

        /// <summary>
        /// Maximum delay for video messages in seconds
        /// </summary>
        [Range(0, int.MaxValue)]
        public int VideoMaxDelaySeconds { get; set; } = 40;
    }

    /// <summary>
    /// Request model for timing configuration in send message request
    /// DEPRECATED: This model is no longer used. Timing is now controlled from the Admin Panel.
    /// See: https://localhost:50645/TimingControl
    /// </summary>
    [Obsolete("Timing is now controlled from the Admin Panel at https://localhost:50645/TimingControl")]
    public class SendMessageTimingRequest
    {
        public int MinDelaySeconds { get; set; } = 5;
        public int MaxDelaySeconds { get; set; } = 15;
        public bool EnableMessageBasedPauses { get; set; } = false;
        public Dictionary<int, int>? MessagePauseRules { get; set; }
        public bool UseStrongRandomization { get; set; } = true;
        public int VideoMinDelaySeconds { get; set; } = 20;
        public int VideoMaxDelaySeconds { get; set; } = 40;
    }
}
