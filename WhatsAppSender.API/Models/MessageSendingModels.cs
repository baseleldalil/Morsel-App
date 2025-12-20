namespace WhatsAppSender.API.Models
{
    /// <summary>
    /// Represents the sending mode for messages
    /// </summary>
    public enum SendingMode
    {
        /// <summary>
        /// Automatically load interval configuration from appsettings.json
        /// </summary>
        Auto,

        /// <summary>
        /// User provides interval configuration via API request
        /// </summary>
        Manual
    }

    /// <summary>
    /// Configuration for message sending intervals
    /// </summary>
    public class SendingIntervalConfig
    {
        /// <summary>
        /// Minimum interval between messages in seconds (must be >= 20)
        /// </summary>
        public int MinIntervalSeconds { get; set; }

        /// <summary>
        /// Maximum interval between messages in seconds (must be >= MinIntervalSeconds)
        /// </summary>
        public int MaxIntervalSeconds { get; set; }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            const int MinimumAllowedInterval = 20;

            if (MinIntervalSeconds < MinimumAllowedInterval)
            {
                errorMessage = $"MinIntervalSeconds must be at least {MinimumAllowedInterval} seconds";
                return false;
            }

            if (MaxIntervalSeconds < MinIntervalSeconds)
            {
                errorMessage = "MaxIntervalSeconds must be greater than or equal to MinIntervalSeconds";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// Request model for sending messages with manual or auto mode
    /// </summary>
    public class SendMessagesWithModeRequest
    {
        /// <summary>
        /// The sending mode to use
        /// </summary>
        public SendingMode Mode { get; set; } = SendingMode.Auto;

        /// <summary>
        /// Phone numbers to send messages to
        /// </summary>
        public List<string> PhoneNumbers { get; set; } = new();

        /// <summary>
        /// Message to send to all phone numbers
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Manual interval configuration (required when Mode = Manual)
        /// </summary>
        public SendingIntervalConfig? ManualConfig { get; set; }
    }

    /// <summary>
    /// Response model for message sending operations
    /// </summary>
    public class SendMessagesWithModeResponse
    {
        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Overall status message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Total number of messages to be sent
        /// </summary>
        public int TotalMessages { get; set; }

        /// <summary>
        /// Number of messages sent successfully
        /// </summary>
        public int SentCount { get; set; }

        /// <summary>
        /// Number of messages that failed to send
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// The sending mode that was used
        /// </summary>
        public SendingMode ModeUsed { get; set; }

        /// <summary>
        /// The interval configuration that was used
        /// </summary>
        public SendingIntervalConfig ConfigUsed { get; set; } = new();

        /// <summary>
        /// Detailed status for each message
        /// </summary>
        public List<MessageSendingDetail> Details { get; set; } = new();

        /// <summary>
        /// List of errors encountered during sending
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Detailed information about a single message send operation
    /// </summary>
    public class MessageSendingDetail
    {
        /// <summary>
        /// Phone number the message was sent to
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the message was sent successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Delay in seconds before sending this message
        /// </summary>
        public double DelaySeconds { get; set; }

        /// <summary>
        /// Timestamp when the message was sent
        /// </summary>
        public DateTime SentAt { get; set; }

        /// <summary>
        /// Error message if sending failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
