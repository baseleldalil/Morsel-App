using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using WhatsAppSender.API.Models;
using System.Text;
using Microsoft.Extensions.Options;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace WhatsAppSender.API.Services
{
    public interface IWhatsAppService
    {
        Task<SendMessageResponse> SendMessagesAsync(SendMessageRequest request, Models.ApiKey apiKey);
        Task<bool> TestConnectionAsync();
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly ILogger<WhatsAppService> _logger;
        private readonly IApiKeyService _apiKeyService;
        private readonly ITimingService _timingService;
        private readonly IDeliveryTrackingService _deliveryTrackingService;
        private readonly IBrowserSessionManager _browserSessionManager;
        private readonly WhatsAppSettings _settings;
        private readonly SaaSDbContext _dbContext;
        private static readonly SemaphoreSlim _semaphore = new(1, 1); // Only one WhatsApp session at a time

        public WhatsAppService(
            ILogger<WhatsAppService> logger,
            IApiKeyService apiKeyService,
            ITimingService timingService,
            IDeliveryTrackingService deliveryTrackingService,
            IBrowserSessionManager browserSessionManager,
            IOptions<WhatsAppSettings> settings,
            SaaSDbContext dbContext)
        {
            _logger = logger;
            _apiKeyService = apiKeyService;
            _timingService = timingService;
            _deliveryTrackingService = deliveryTrackingService;
            _browserSessionManager = browserSessionManager;
            _settings = settings.Value;
            _dbContext = dbContext;
        }

        public async Task<SendMessageResponse> SendMessagesAsync(SendMessageRequest request, Models.ApiKey apiKey)
        {
            var response = new SendMessageResponse();

            //// Check quota before starting
            //var hasQuota = await _apiKeyService.CheckQuotaAsync(apiKey.Id);
            //if (!hasQuota)
            //{
            //    response.Success = false;
            //    response.Message = "Daily quota exceeded. Please upgrade your plan or wait until tomorrow.";
            //    response.RemainingQuota = 0;
            //    return response;
            //}

            // Calculate total messages needed
            int totalMessages = request.Messages.Sum(m => m.Messages.Count + m.Files.Count);

            // Check if user has enough quota
            var usageStats = await _apiKeyService.GetUsageStatsAsync(apiKey.KeyValue);
            //if (usageStats.RemainingQuota < totalMessages)
            ////{
            ////    response.Success = false;
            ////    response.Message = $"Not enough quota. Need {totalMessages} messages, but only {usageStats.RemainingQuota} remaining.";
            ////    response.RemainingQuota = usageStats.RemainingQuota;
            ////    return response;
            ////}

            await _semaphore.WaitAsync();
            IWebDriver? driver = null;
            bool shouldDisposeDriver = false;

            try
            {
                _logger.LogInformation("Starting WhatsApp session for user: {UserEmail}", apiKey.UserEmail);

                // Get browser settings from request or use defaults
                var browserSettings = request.BrowserSettings ?? new BrowserSettings
                {
                    Type = BrowserType.Chrome,
                    KeepSessionOpen = true
                };

                // Get or create browser session
                driver = await _browserSessionManager.GetOrCreateBrowserAsync(apiKey.UserEmail, browserSettings);
                shouldDisposeDriver = !browserSettings.KeepSessionOpen;

                // Only navigate to WhatsApp Web if we're on a different URL
                if (!driver.Url.Contains("web.whatsapp.com"))
                {
                    _logger.LogInformation("Navigating to WhatsApp Web for user: {UserEmail}", apiKey.UserEmail);
                    driver.Navigate().GoToUrl("https://web.whatsapp.com");
                }
                else
                {
                    _logger.LogInformation("Reusing existing WhatsApp Web session for user: {UserEmail}", apiKey.UserEmail);
                }

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(_settings.WebDriverWaitSeconds.GetRandomValue()));

                // Wait for WhatsApp to load
                if (!await WaitForWhatsAppLoadAsync(driver, wait))
                {
                    response.Success = false;
                    response.Message = "WhatsApp Web did not load. Please scan QR code or check your internet connection.";
                    return response;
                }

                _logger.LogInformation("WhatsApp Web loaded successfully");

                // Get subscription plan ID from API key for timing control
                int? subscriptionPlanId = apiKey.SubscriptionId;
                _logger.LogInformation("Using subscription plan ID {SubscriptionId} for timing control", subscriptionPlanId);

                int messagesSentCount = 0;

                // Process messages
                foreach (var messageItem in request.Messages)
                {
                    try
                    {
                        // Apply realistic random delay between messages (skip first)
                        if (messagesSentCount == 0)
                        {
                            int delaySeconds = GetSmartRandomDelaySeconds(messagesSentCount);
                            _logger.LogInformation(
                                "Applying smart random delay: {DelaySeconds}s before message #{MessageNumber}",
                                delaySeconds, messagesSentCount + 1);

                            await Task.Delay(delaySeconds * 1000);
                        }

                        // Check for additional pause rules from database (Admin Panel Timing Control)
                        var randomDelayRule = await _timingService.GetApplicableDelayRuleAsync(
                            messagesSentCount,
                            subscriptionPlanId);

                        if (randomDelayRule != null)
                        {
                            // Calculate pause duration with randomization
                            int pauseMinutes = randomDelayRule.PauseDurationMinutes;
                            int varianceSeconds = randomDelayRule.RandomVarianceSeconds;

                            // Add random variance
                            var random = new Random();
                            int variance = random.Next(-varianceSeconds, varianceSeconds + 1);
                            int totalPauseSeconds = (pauseMinutes * 60) + variance;

                            _logger.LogInformation("Random delay rule '{RuleName}' triggered after {Count} messages. Pausing for {Duration} seconds",
                                randomDelayRule.Name, messagesSentCount, totalPauseSeconds);

                            await Task.Delay(totalPauseSeconds * 1000);
                        }

                        // Process single recipient with delivery tracking
                        var deliveryStatus = await ProcessSingleRecipient(driver, wait, messageItem, subscriptionPlanId);
                        response.DeliveryStatuses.Add(deliveryStatus);

                        if (deliveryStatus.Status == "Sent" || deliveryStatus.Status == "Delivered" || deliveryStatus.Status == "Read")
                        {
                            response.ProcessedCount++;
                            response.SentCount++;

                            if (deliveryStatus.Status == "Delivered" || deliveryStatus.Status == "Read")
                            {
                                response.DeliveredCount++;
                            }

                            // Update contact status in database
                            var contactStatus = (deliveryStatus.Status == "Delivered" || deliveryStatus.Status == "Read")
                                ? ContactStatus.Delivered
                                : ContactStatus.Sent;

                            await UpdateContactStatusAsync(
                                messageItem.Phone,
                                apiKey.UserId,
                                contactStatus,
                                null);
                        }
                        else
                        {
                            response.FailedCount++;
                            response.Errors.Add($"Failed to send to {messageItem.Phone}: {deliveryStatus.ErrorMessage ?? "Unknown error"}");

                            // Update contact status to Failed
                            await UpdateContactStatusAsync(
                                messageItem.Phone,
                                apiKey.UserId,
                                ContactStatus.Failed,
                                deliveryStatus.ErrorMessage);
                        }

                        messagesSentCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending to {Phone}", messageItem.Phone);
                        response.FailedCount++;
                        response.Errors.Add($"Failed to send to {messageItem.Phone}: {ex.Message}");

                        response.DeliveryStatuses.Add(new MessageDeliveryStatus
                        {
                            Phone = messageItem.Phone,
                            Status = "Failed",
                            ErrorMessage = ex.Message
                        });

                        await UpdateContactStatusAsync(
                            messageItem.Phone,
                            apiKey.UserId,
                            ContactStatus.Failed,
                            ex.Message);
                    }
                }

                // Update usage
                await _apiKeyService.UpdateUsageAsync(apiKey.Id, totalMessages);

                // Get updated quota
                var updatedStats = await _apiKeyService.GetUsageStatsAsync(apiKey.KeyValue);
                response.RemainingQuota = updatedStats.RemainingQuota;

                response.Success = response.ProcessedCount > 0;
                response.Message = response.Success ?
                    $"Successfully sent messages to {response.ProcessedCount} recipients" :
                    "Failed to send any messages";

                _logger.LogInformation("WhatsApp session completed. Processed: {Processed}, Failed: {Failed}",
                    response.ProcessedCount, response.FailedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WhatsApp service");
                response.Success = false;
                response.Message = $"Service error: {ex.Message}";
            }
            finally
            {
                if (shouldDisposeDriver && driver != null)
                {
                    try
                    {
                        driver.Quit();
                        _logger.LogInformation("Disposed browser session for user: {UserEmail}", apiKey.UserEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing browser for user: {UserEmail}", apiKey.UserEmail);
                    }
                }
                else if (driver != null)
                {
                    _logger.LogInformation("Keeping browser session open for user: {UserEmail}", apiKey.UserEmail);
                }

                _semaphore.Release();
            }

            return response;
        }


        // ------------------ SMART RANDOM DELAY FUNCTION ------------------

        private static readonly Random _random = new Random();

        private int GetSmartRandomDelaySeconds(int messageIndex)
        {
            // Base delay increases slowly with message count
            double baseDelay = 1.5 + Math.Log(messageIndex + 1) * 1.2;

            // Add irregular noise (±60%)
            double noiseFactor = 1 + (_random.NextDouble() - 0.5) * 1.2;

            // Occasionally add a "spike" pause
            bool spike = _random.NextDouble() < 0.15; // 15% chance
            double spikeMultiplier = spike ? (1.5 + _random.NextDouble()) : 1.0;

            double finalDelay = baseDelay * noiseFactor * spikeMultiplier;

            // Clamp values (1s to 20s)
            finalDelay = Math.Clamp(finalDelay, 1.0, 20.0);

            return (int)Math.Round(finalDelay);
        }


        public async Task<bool> TestConnectionAsync()
        {
            await Task.CompletedTask;
            return true; // For now, always return true
        }

        private async Task<bool> WaitForWhatsAppLoadAsync(IWebDriver driver, WebDriverWait wait)
        {
            try
            {
                // Wait for main input to be available (indicates WhatsApp is loaded)
                string[] selectors = {
                    "div[role='textbox'][contenteditable='true']",
                    "footer div[contenteditable='true']",
                    "div[contenteditable='true']"
                };

                for (int attempt = 0; attempt < _settings.WhatsAppLoadMaxAttempts; attempt++)
                {
                    foreach (var selector in selectors)
                    {
                        try
                        {
                            var element = driver.FindElement(By.CssSelector(selector));
                            if (element != null)
                            {
                                return true;
                            }
                        }
                        catch { }
                    }

                    await Task.Delay(_settings.WhatsAppLoadCheckIntervalSeconds.GetRandomValue() * 1000);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for WhatsApp to load");
                return false;
            }
        }

        private async Task<MessageDeliveryStatus> ProcessSingleRecipient(IWebDriver driver, WebDriverWait wait, WhatsAppMessage messageItem, int? subscriptionPlanId)
        {
            var deliveryStatus = new MessageDeliveryStatus
            {
                Phone = messageItem.Phone,
                Status = "Pending"
            };

            try
            {
                string url = $"https://web.whatsapp.com/send?phone={messageItem.Phone}";
                driver.Navigate().GoToUrl(url);
                await Task.Delay(_settings.NavigationDelayMilliseconds.GetRandomValue());

                // Find message box
                IWebElement? messageBox = null;
                string[] selectors = {
                    "div[contenteditable='true'][data-tab='10']",
                    "div[contenteditable='true'][data-tab='6']",
                    "footer div[contenteditable='true']",
                    "div[role='textbox'][contenteditable='true']"
                };

                foreach (var selector in selectors)
                {
                    try
                    {
                        messageBox = wait.Until(d => d.FindElement(By.CssSelector(selector)));
                        if (messageBox != null) break;
                    }
                    catch { }
                }

                if (messageBox == null)
                {
                    deliveryStatus.Status = "Failed";
                    deliveryStatus.ErrorMessage = $"Could not find message input for {messageItem.Phone}";
                    return deliveryStatus;
                }

                // Track if first message was used as caption
                bool firstMessageUsedAsCaption = false;
                bool hasVideo = messageItem.Files?.Any(f => f.FileType?.StartsWith("video/") == true) ?? false;

                // Send files if provided
                if (messageItem.Files != null && messageItem.Files.Any())
                {
                    firstMessageUsedAsCaption = await SendMultipleFilesAsync(driver, wait, messageItem);
                }

                // Send text messages (skip first one if it was used as caption)
                var messagesToSend = firstMessageUsedAsCaption
                    ? messageItem.Messages.Skip(1).ToList()
                    : messageItem.Messages;

                foreach (var message in messagesToSend)
                {
                    // Use JavaScript execution for better emoji support
                    await SendTextToMessageBox(driver, messageBox, message);

                    // Press Enter to send
                    messageBox.SendKeys(Keys.Enter);

                    // Wait for sent confirmation
                    deliveryStatus.SentAt = DateTime.UtcNow;
                    bool isSent = await _deliveryTrackingService.WaitForSentConfirmationAsync(driver, _settings.SentConfirmationTimeoutSeconds.GetRandomValue());

                    if (!isSent)
                    {
                        _logger.LogWarning("No sent confirmation for {Phone}", messageItem.Phone);
                    }

                    await Task.Delay(_settings.MessageSendDelayMilliseconds.GetRandomValue());
                }

                // Check final delivery status
                string finalStatus = await _deliveryTrackingService.CheckDeliveryStatusAsync(driver, messageItem.Phone);
                deliveryStatus.Status = finalStatus;

                if (finalStatus == "Delivered" || finalStatus == "Read")
                {
                    deliveryStatus.DeliveredAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Messages sent to {Phone} with status: {Status}", messageItem.Phone, finalStatus);

                // Note: Delay between recipients is now handled at the beginning of the message loop
                // This ensures proper random timing between phone numbers
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recipient {Phone}", messageItem.Phone);
                deliveryStatus.Status = "Failed";
                deliveryStatus.ErrorMessage = ex.Message;
            }

            return deliveryStatus;
        }

        private async Task<bool> SendMultipleFilesAsync(IWebDriver driver, WebDriverWait wait, WhatsAppMessage messageItem)
        {
            bool captionAdded = false;

            try
            {
                // Convert all base64 files to temp files
                var tempFilePaths = new List<string>();

                foreach (var file in messageItem.Files)
                {
                    var bytes = Convert.FromBase64String(file.FileBase64);
                    var extension = GetFileExtension(file.FileType ?? "image/png");
                    var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
                    await File.WriteAllBytesAsync(tempPath, bytes);
                    tempFilePaths.Add(tempPath);
                }

                // Click the plus (attachment) icon
                IWebElement plusIcon = wait.Until(d =>
                    d.FindElement(By.CssSelector("span[data-icon='plus-rounded']"))
                );
                plusIcon.Click();
                await Task.Delay(_settings.AttachmentClickDelayMilliseconds.GetRandomValue());

                // Locate the file input and upload all files at once
                IWebElement? fileInput = null;

                string[] fileInputSelectors = {
                    "input[type='file'][accept*='image']",
                    "input[type='file'][accept*='video']",
                    "input[type='file']"
                };

                foreach (var selector in fileInputSelectors)
                {
                    try
                    {
                        fileInput = wait.Until(d => d.FindElement(By.CssSelector(selector)));
                        if (fileInput != null) break;
                    }
                    catch { }
                }

                if (fileInput != null)
                {
                    // Upload all files at once (separated by newlines)
                    string allFiles = string.Join("\n", tempFilePaths);
                    fileInput.SendKeys(allFiles);
                    await Task.Delay(_settings.FileUploadDelayMilliseconds.GetRandomValue());

                    // Add caption if first message exists
                    if (messageItem.Messages.Any())
                    {
                        try
                        {
                            IWebElement editableDiv = driver.FindElement(By.CssSelector("div[contenteditable='true'][aria-placeholder='Type a message']"));
                            // Use JavaScript execution for better emoji support in caption
                            await SendTextToMessageBox(driver, editableDiv, messageItem.Messages[0]);
                            await Task.Delay(_settings.CaptionDelayMilliseconds.GetRandomValue());
                            captionAdded = true; // Mark that we used the first message as caption
                            _logger.LogInformation("Caption added to files for {Phone}", messageItem.Phone);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not add caption to files for {Phone}", messageItem.Phone);
                        }
                    }

                    // Click the send button
                    IWebElement sendBtn = wait.Until(d =>
                        d.FindElement(By.CssSelector("div[role='button'][aria-label='Send']"))
                    );

                    // Extra delay for videos
                    if (messageItem.Files.Any(f => f.FileType?.StartsWith("video/") == true))
                    {
                        await Task.Delay(_settings.VideoExtraDelayMilliseconds.GetRandomValue());
                    }

                    sendBtn.Click();
                    await Task.Delay(_settings.SendButtonDelayMilliseconds.GetRandomValue());

                    _logger.LogInformation("Files sent successfully to {Phone}", messageItem.Phone);
                }

                // Clean up temp files
                foreach (var tempPath in tempFilePaths)
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error sending files to {Phone}: {Error}", messageItem.Phone, ex.Message);
            }

            return captionAdded; // Return whether we used the first message as caption
        }

        private static string GetFileExtension(string mimeType)
        {
            return mimeType.ToLowerInvariant() switch
            {
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "video/mp4" => ".mp4",
                "video/quicktime" => ".mov",
                "application/pdf" => ".pdf",
                _ => ".png"
            };
        }

        /// <summary>
        /// Normalizes message text for WhatsApp - keeps emojis as-is
        /// </summary>
        private static string NormalizeMessageForWhatsApp(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // Simply return the message as-is - .NET handles Unicode properly
            // No need for extra encoding that might corrupt emojis
            return message;
        }

        /// <summary>
        /// Sends text to WhatsApp message box using JavaScript to preserve emojis
        /// JavaScript method ensures emojis remain as actual Unicode characters
        /// </summary>
        private async Task SendTextToMessageBox(IWebDriver driver, IWebElement messageBox, string text)
        {
            try
            {
                var jsExecutor = (IJavaScriptExecutor)driver;

                // Use JavaScript to set text - this preserves emojis perfectly
                string script = @"
                    var element = arguments[0];
                    var text = arguments[1];

                    // Focus the element
                    element.focus();
                    element.click();

                    // Clear existing content to prevent any residual text
                    element.innerHTML = '';

                    // Create a text node - this preserves all Unicode including emojis
                    var textNode = document.createTextNode(text);
                    element.appendChild(textNode);

                    // Trigger ONLY input event for WhatsApp React to detect the change
                    // Removed keyup event to prevent double-triggering
                    var inputEvent = new InputEvent('input', {
                        bubbles: true,
                        cancelable: true,
                        inputType: 'insertText',
                        data: text
                    });
                    element.dispatchEvent(inputEvent);

                    // Set cursor to end
                    var range = document.createRange();
                    var sel = window.getSelection();
                    if (element.childNodes.length > 0) {
                        range.setStart(element.childNodes[0], element.childNodes[0].length);
                        range.collapse(true);
                        sel.removeAllRanges();
                        sel.addRange(range);
                    }
                    element.focus();
                ";

                jsExecutor.ExecuteScript(script, messageBox, text);
                await Task.Delay(_settings.JavaScriptExecutionDelayMilliseconds.GetRandomValue());

                _logger.LogDebug("Text sent successfully using JavaScript - emojis preserved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send text using JavaScript");

                // Fallback: Try using SendKeys (may not work with emojis)
                try
                {
                    messageBox.Click();
                    await Task.Delay(_settings.FallbackDelayMilliseconds.GetRandomValue());
                    messageBox.SendKeys(text);
                    await Task.Delay(_settings.FallbackDelayMilliseconds.GetRandomValue());

                    _logger.LogWarning("Used SendKeys fallback - emojis may not display correctly");
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "All methods failed to send text");
                    throw;
                }
            }
        }

        /// <summary>
        /// Updates the contact status in the database after sending
        /// </summary>
        private async Task UpdateContactStatusAsync(
            string phoneNumber,
            string userId,
            ContactStatus status,
            string? errorMessage)
        {
            try
            {
                // Find contact by phone number and user ID
                var contact = await _dbContext.Contacts
                    .FirstOrDefaultAsync(c => c.FormattedPhone == phoneNumber && c.UserId == userId);

                if (contact != null)
                {
                    // Update status and timestamps
                    contact.Status = status;
                    contact.LastStatusUpdateAt = DateTime.UtcNow;
                    contact.SendAttemptCount++;
                    contact.UpdatedAt = DateTime.UtcNow;

                    // Update last message sent time if successful
                    if (status == ContactStatus.Sent || status == ContactStatus.Delivered)
                    {
                        contact.LastMessageSentAt = DateTime.UtcNow;
                    }

                    // Set issue description if failed
                    if (status == ContactStatus.Failed || status == ContactStatus.HasIssues)
                    {
                        contact.IssueDescription = errorMessage?.Length > 500
                            ? errorMessage.Substring(0, 500)
                            : errorMessage;
                    }
                    else
                    {
                        // Clear issue description on success
                        contact.IssueDescription = null;
                    }

                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "Updated contact status: Phone={Phone}, Status={Status}, Attempts={Attempts}",
                        phoneNumber,
                        status,
                        contact.SendAttemptCount);
                }
                else
                {
                    _logger.LogWarning(
                        "Contact not found for status update: Phone={Phone}, UserId={UserId}",
                        phoneNumber,
                        userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating contact status: Phone={Phone}, Status={Status}",
                    phoneNumber,
                    status);
                // Don't throw - status update failure shouldn't stop message sending
            }
        }

        /// <summary>
        /// Calculates progressive random delay in SECONDS (30-60 seconds range)
        /// Examples: 33, 47, 56, 38, 52, 41, 59 seconds
        /// Pattern: Random variation within 30-60 second range with slight progressive tendency
        /// </summary>
        private int CalculateProgressiveDelay(int messageCount)
        {
            var random = Random.Shared;

            // Base delay starts at 45 seconds (middle of 30-60 range)
            double baseDelay = 45.0;

            // Add slight progressive increase (max +5 seconds over many messages)
            double progressiveBoost = Math.Min(Math.Log(messageCount + 1) * 0.8, 5.0);
            baseDelay += progressiveBoost;

            // Add random variance to spread across 30-60 range
            // Random factor between -1.0 and +1.0
            double randomFactor = (random.NextDouble() * 2.0) - 1.0;

            // Apply variance: ±15 seconds from base
            double variance = randomFactor * 15.0;
            double delaySeconds = baseDelay + variance;

            // Occasionally apply a "reset" to lower end (15% chance)
            if (random.Next(100) < 15)
            {
                delaySeconds = 30.0 + (random.NextDouble() * 10.0); // 30-40 seconds
            }

            // Enforce strict range: 30-60 seconds
            delaySeconds = Math.Max(delaySeconds, 30.0);
            delaySeconds = Math.Min(delaySeconds, 60.0);

            return (int)Math.Round(delaySeconds);
        }
    }
}
