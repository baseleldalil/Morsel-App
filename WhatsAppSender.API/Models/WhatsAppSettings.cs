namespace WhatsAppSender.API.Models
{
    /// <summary>
    /// Represents a time range with minimum and maximum values
    /// </summary>
    public class TimeRange
    {
        /// <summary>
        /// Minimum value
        /// </summary>
        public int Min { get; set; }

        /// <summary>
        /// Maximum value
        /// </summary>
        public int Max { get; set; }

        /// <summary>
        /// Gets a random value within the range
        /// </summary>
        public int GetRandomValue()
        {
            if (Min == Max) return Min;
            return Random.Shared.Next(Min, Max + 1);
        }

        /// <summary>
        /// Validates the range
        /// </summary>
        public bool IsValid() => Min >= 0 && Max >= Min;
    }

    /// <summary>
    /// Configuration settings for WhatsApp service loaded from appsettings.json
    /// </summary>
    public class WhatsAppSettings
    {
        public const string SectionName = "WhatsAppSettings";

        /// <summary>
        /// Maximum number of concurrent WhatsApp sessions (default: 1)
        /// </summary>
        public int MaxConcurrentSessions { get; set; } = 1;

        /// <summary>
        /// Session timeout in minutes (default: 30)
        /// </summary>
        public int SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// WebDriver wait timeout in seconds range (default: 100-120)
        /// </summary>
        public TimeRange WebDriverWaitSeconds { get; set; } = new() { Min = 100, Max = 120 };

        /// <summary>
        /// Interval between WhatsApp load checks in seconds range (default: 4-6)
        /// </summary>
        public TimeRange WhatsAppLoadCheckIntervalSeconds { get; set; } = new() { Min = 4, Max = 6 };

        /// <summary>
        /// Maximum number of attempts to check if WhatsApp is loaded (default: 24)
        /// </summary>
        public int WhatsAppLoadMaxAttempts { get; set; } = 24;

        /// <summary>
        /// Delay after navigation in milliseconds range (default: 6000-10000)
        /// </summary>
        public TimeRange NavigationDelayMilliseconds { get; set; } = new() { Min = 6000, Max = 10000 };

        /// <summary>
        /// Timeout for sent confirmation in seconds range (default: 12-18)
        /// </summary>
        public TimeRange SentConfirmationTimeoutSeconds { get; set; } = new() { Min = 12, Max = 18 };

        /// <summary>
        /// Delay between messages in milliseconds range (default: 3000-7000)
        /// </summary>
        public TimeRange MessageSendDelayMilliseconds { get; set; } = new() { Min = 3000, Max = 7000 };

        /// <summary>
        /// Delay after clicking attachment button in milliseconds range (default: 8000-12000)
        /// </summary>
        public TimeRange AttachmentClickDelayMilliseconds { get; set; } = new() { Min = 8000, Max = 12000 };

        /// <summary>
        /// Delay after file upload in milliseconds range (default: 8000-12000)
        /// </summary>
        public TimeRange FileUploadDelayMilliseconds { get; set; } = new() { Min = 8000, Max = 12000 };

        /// <summary>
        /// Delay after adding caption in milliseconds range (default: 8000-12000)
        /// </summary>
        public TimeRange CaptionDelayMilliseconds { get; set; } = new() { Min = 8000, Max = 12000 };

        /// <summary>
        /// Extra delay for video uploads in milliseconds range (default: 8000-15000)
        /// </summary>
        public TimeRange VideoExtraDelayMilliseconds { get; set; } = new() { Min = 8000, Max = 15000 };

        /// <summary>
        /// Delay after clicking send button in milliseconds range (default: 8000-12000)
        /// </summary>
        public TimeRange SendButtonDelayMilliseconds { get; set; } = new() { Min = 8000, Max = 12000 };

        /// <summary>
        /// Delay after JavaScript execution in milliseconds range (default: 800-1500)
        /// </summary>
        public TimeRange JavaScriptExecutionDelayMilliseconds { get; set; } = new() { Min = 800, Max = 1500 };

        /// <summary>
        /// Fallback delay for SendKeys in milliseconds range (default: 400-800)
        /// </summary>
        public TimeRange FallbackDelayMilliseconds { get; set; } = new() { Min = 400, Max = 800 };
    }

    /// <summary>
    /// Configuration settings for API key service
    /// </summary>
    public class ApiKeySettings
    {
        public const string SectionName = "ApiKeySettings";

        /// <summary>
        /// Cache expiration time in minutes (default: 15)
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 15;
    }

    /// <summary>
    /// Configuration settings for translation service
    /// </summary>
    public class TranslationSettings
    {
        public const string SectionName = "Translation";

        /// <summary>
        /// HTTP client timeout in seconds (default: 30)
        /// </summary>
        public int HttpClientTimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Configuration settings for contacts controller
    /// </summary>
    public class ContactsSettings
    {
        public const string SectionName = "ContactsSettings";

        /// <summary>
        /// Translation request timeout in seconds (default: 10)
        /// </summary>
        public int TranslationTimeoutSeconds { get; set; } = 10;
    }
}
