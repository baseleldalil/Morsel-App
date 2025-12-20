using Microsoft.Extensions.Options;
using WhatsAppSender.API.Models;

namespace WhatsAppSender.API.Services
{
    /// <summary>
    /// Service interface for sending messages with configurable intervals
    /// </summary>
    public interface IMessageSendingService
    {
        /// <summary>
        /// Sends messages to multiple phone numbers with random intervals
        /// </summary>
        /// <param name="request">The sending request containing mode and configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response with sending details</returns>
        Task<SendMessagesWithModeResponse> SendMessagesAsync(
            SendMessagesWithModeRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current auto mode configuration
        /// </summary>
        /// <returns>The auto mode configuration</returns>
        SendingIntervalConfig GetAutoModeConfig();

        /// <summary>
        /// Validates a sending interval configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateConfig(SendingIntervalConfig config, out string errorMessage);
    }

    /// <summary>
    /// Configuration class for Auto mode settings in appsettings.json
    /// </summary>
    public class AutoModeSendingSettings
    {
        public const string SectionName = "AutoModeSending";

        /// <summary>
        /// Minimum interval between messages in seconds (default: 30)
        /// </summary>
        public int MinIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum interval between messages in seconds (default: 60)
        /// </summary>
        public int MaxIntervalSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Service implementation for sending messages with configurable intervals
    /// Supports both Auto mode (from appsettings.json) and Manual mode (from API request)
    /// </summary>
    public class MessageSendingService : IMessageSendingService
    {
        private readonly AutoModeSendingSettings _autoSettings;
        private readonly ILogger<MessageSendingService> _logger;
        private readonly Random _random;

        private const int MinimumAllowedInterval = 20;

        public MessageSendingService(
            IOptions<AutoModeSendingSettings> autoSettings,
            ILogger<MessageSendingService> logger)
        {
            _autoSettings = autoSettings.Value;
            _logger = logger;
            _random = new Random();

            // Validate auto settings on startup
            ValidateAutoSettings();
        }

        /// <summary>
        /// Validates the auto mode settings from appsettings.json
        /// </summary>
        private void ValidateAutoSettings()
        {
            var config = new SendingIntervalConfig
            {
                MinIntervalSeconds = _autoSettings.MinIntervalSeconds,
                MaxIntervalSeconds = _autoSettings.MaxIntervalSeconds
            };

            if (!config.IsValid(out var errorMessage))
            {
                _logger.LogWarning(
                    "Invalid Auto mode configuration in appsettings.json: {Error}. Using defaults: Min=30s, Max=60s",
                    errorMessage);

                _autoSettings.MinIntervalSeconds = 30;
                _autoSettings.MaxIntervalSeconds = 60;
            }
        }

        /// <summary>
        /// Gets the current auto mode configuration
        /// </summary>
        public SendingIntervalConfig GetAutoModeConfig()
        {
            return new SendingIntervalConfig
            {
                MinIntervalSeconds = _autoSettings.MinIntervalSeconds,
                MaxIntervalSeconds = _autoSettings.MaxIntervalSeconds
            };
        }

        /// <summary>
        /// Validates a sending interval configuration
        /// </summary>
        public bool ValidateConfig(SendingIntervalConfig config, out string errorMessage)
        {
            return config.IsValid(out errorMessage);
        }

        /// <summary>
        /// Sends messages to multiple phone numbers with random intervals
        /// </summary>
        public async Task<SendMessagesWithModeResponse> SendMessagesAsync(
            SendMessagesWithModeRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = new SendMessagesWithModeResponse
            {
                ModeUsed = request.Mode,
                TotalMessages = request.PhoneNumbers.Count
            };

            try
            {
                // Step 1: Validate request
                if (!ValidateRequest(request, out var validationError))
                {
                    response.Success = false;
                    response.Message = validationError;
                    response.Errors.Add(validationError);
                    return response;
                }

                // Step 2: Determine which configuration to use
                var config = GetConfigurationForMode(request);
                response.ConfigUsed = config;

                _logger.LogInformation(
                    "Starting message sending in {Mode} mode. Config: Min={Min}s, Max={Max}s, Total messages: {Count}",
                    request.Mode,
                    config.MinIntervalSeconds,
                    config.MaxIntervalSeconds,
                    request.PhoneNumbers.Count);

                // Step 3: Send messages with random delays
                for (int i = 0; i < request.PhoneNumbers.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Message sending cancelled by user");
                        response.Message = "Sending cancelled by user";
                        response.Errors.Add("Operation cancelled");
                        break;
                    }

                    var phoneNumber = request.PhoneNumbers[i];
                    var detail = new MessageSendingDetail
                    {
                        PhoneNumber = phoneNumber
                    };

                    try
                    {
                        // Calculate random delay for this message
                        var delaySeconds = CalculateRandomDelay(config);
                        detail.DelaySeconds = delaySeconds;

                        // Wait for the calculated delay before sending
                        // (skip delay for the first message)
                        if (i > 0)
                        {
                            _logger.LogInformation(
                                "Waiting {Delay:F2} seconds before sending message {Current}/{Total} to {Phone}",
                                delaySeconds,
                                i + 1,
                                request.PhoneNumbers.Count,
                                MaskPhoneNumber(phoneNumber));

                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Sending first message (no delay) to {Phone}",
                                MaskPhoneNumber(phoneNumber));
                        }

                        // Send the message
                        await SendSingleMessageAsync(phoneNumber, request.Message, cancellationToken);

                        detail.Success = true;
                        detail.SentAt = DateTime.UtcNow;
                        response.SentCount++;

                        _logger.LogInformation(
                            "Message {Current}/{Total} sent successfully to {Phone}",
                            i + 1,
                            request.PhoneNumbers.Count,
                            MaskPhoneNumber(phoneNumber));
                    }
                    catch (Exception ex)
                    {
                        detail.Success = false;
                        detail.ErrorMessage = ex.Message;
                        response.FailedCount++;
                        response.Errors.Add($"Failed to send to {MaskPhoneNumber(phoneNumber)}: {ex.Message}");

                        _logger.LogError(
                            ex,
                            "Failed to send message to {Phone}",
                            MaskPhoneNumber(phoneNumber));
                    }

                    response.Details.Add(detail);
                }

                // Step 4: Build final response
                response.Success = response.SentCount > 0;
                response.Message = response.Success
                    ? $"Successfully sent {response.SentCount} of {response.TotalMessages} messages in {request.Mode} mode"
                    : "Failed to send any messages";

                _logger.LogInformation(
                    "Message sending completed. Sent: {Sent}, Failed: {Failed}",
                    response.SentCount,
                    response.FailedCount);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during message sending");
                response.Success = false;
                response.Message = "An unexpected error occurred";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        /// <summary>
        /// Validates the incoming request
        /// </summary>
        private bool ValidateRequest(SendMessagesWithModeRequest request, out string errorMessage)
        {
            if (request.PhoneNumbers == null || request.PhoneNumbers.Count == 0)
            {
                errorMessage = "Phone numbers list cannot be empty";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                errorMessage = "Message cannot be empty";
                return false;
            }

            if (request.Mode == SendingMode.Manual)
            {
                if (request.ManualConfig == null)
                {
                    errorMessage = "ManualConfig is required when Mode is Manual";
                    return false;
                }

                if (!request.ManualConfig.IsValid(out errorMessage))
                {
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Gets the appropriate configuration based on the sending mode
        /// </summary>
        private SendingIntervalConfig GetConfigurationForMode(SendMessagesWithModeRequest request)
        {
            if (request.Mode == SendingMode.Manual)
            {
                return request.ManualConfig!;
            }

            // Auto mode - use appsettings.json configuration
            return GetAutoModeConfig();
        }

        /// <summary>
        /// Calculates a random delay between min and max intervals
        /// The delay varies randomly within the specified range to mimic human behavior
        /// </summary>
        private double CalculateRandomDelay(SendingIntervalConfig config)
        {
            // Generate random delay between min and max
            // Using NextDouble() for more granular randomization
            var range = config.MaxIntervalSeconds - config.MinIntervalSeconds;
            var randomFactor = _random.NextDouble(); // 0.0 to 1.0
            var delay = config.MinIntervalSeconds + (range * randomFactor);

            // Ensure minimum delay is respected
            if (delay < MinimumAllowedInterval)
            {
                delay = MinimumAllowedInterval;
            }

            return Math.Round(delay, 2); // Round to 2 decimal places
        }

        /// <summary>
        /// Sends a single message to a phone number
        /// This is a placeholder - in production, integrate with WhatsAppService or similar
        /// </summary>
        private async Task SendSingleMessageAsync(
            string phoneNumber,
            string message,
            CancellationToken cancellationToken)
        {
            // TODO: Integrate with existing WhatsAppService or actual sending logic
            // For now, simulate sending delay
            await Task.Delay(100, cancellationToken);

            // Example integration point:
            // await _whatsAppService.SendMessageAsync(phoneNumber, message);

            _logger.LogDebug("Simulated sending message to {Phone}", MaskPhoneNumber(phoneNumber));
        }

        /// <summary>
        /// Masks a phone number for logging (shows only last 4 digits)
        /// </summary>
        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length <= 4)
            {
                return "****";
            }

            var lastFour = phoneNumber.Substring(phoneNumber.Length - 4);
            return $"****{lastFour}";
        }
    }
}
