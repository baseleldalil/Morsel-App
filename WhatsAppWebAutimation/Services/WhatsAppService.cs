using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using WhatsAppWebAutomation.Data;
using WhatsAppWebAutomation.DTOs;
using WhatsAppWebAutomation.Helpers;

namespace WhatsAppWebAutomation.Services;

/// <summary>
/// Contact status values for the Contacts table
/// </summary>
public static class ContactStatus
{
    public const int Pending = 0;
    public const int Failed = 1;
    public const int Delivered = 3;
}

/// <summary>
/// WhatsApp Web automation service using Selenium
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly IBrowserService _browserService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly IBulkOperationManager _bulkManager;
    private readonly IServiceProvider _serviceProvider;

    // WhatsApp Web URL
    private const string WHATSAPP_URL = "https://web.whatsapp.com";

    /// <summary>
    /// Selenium Selectors for WhatsApp Web (updated for 2024)
    /// </summary>
    private static class Selectors
    {
        // QR Code / Login check
        public const string QR_CODE = "canvas[aria-label='Scan this QR code to link a device!']";
        public const string QR_CODE_ALT = "canvas";
        public const string SIDE_PANEL = "div#side";
        public const string CHAT_LIST = "div[aria-label='Chat list']";
        public const string MAIN_CONTAINER = "div#app";

        // Search and navigation
        public const string SEARCH_BOX = "div[contenteditable='true'][data-tab='3']";
        public const string NEW_CHAT_BUTTON = "div[title='New chat']";

        // Message input and send
        public const string MESSAGE_INPUT = "div[contenteditable='true'][data-tab='10']";
        public const string MESSAGE_INPUT_ALT = "footer div[contenteditable='true']";
        public const string SEND_BUTTON = "span[data-icon='send']";
        public const string SEND_BUTTON_ALT = "button[aria-label='Send']";

        // Attachment buttons (updated 2024/2025)
        public const string ATTACH_BUTTON = "span[data-icon='plus-rounded']";
        public const string ATTACH_BUTTON_ALT = "span[data-icon='attach-menu-plus']";
        public const string ATTACH_BUTTON_PLUS = "div[title='Attach']";
        public const string ATTACH_CLIP = "span[data-icon='clip']";

        // Attachment menu items (WhatsApp Web 2024/2025 UI)
        // Document option - for PDF, Word, Excel, etc.
        public const string MENU_DOCUMENT = "li[role='button'] svg[viewBox='0 0 24 24'] title";
        public const string MENU_DOCUMENT_ICON = "span[aria-hidden='true'] svg title";
        public const string MENU_DOCUMENT_TEXT = "//li[@role='button']//span[text()='Document']";

        // Photos & videos option
        public const string MENU_PHOTOS_VIDEOS = "//li[@role='button']//span[text()='Photos & videos']";
        public const string MENU_PHOTOS_ICON = "svg title[text()='ic-filter-filled']";

        // Camera option
        public const string MENU_CAMERA = "//li[@role='button']//span[text()='Camera']";

        // Audio option
        public const string MENU_AUDIO = "//li[@role='button']//span[text()='Audio']";

        // Contact option
        public const string MENU_CONTACT = "//li[@role='button']//span[text()='Contact']";

        // Poll option
        public const string MENU_POLL = "//li[@role='button']//span[text()='Poll']";

        // Event option
        public const string MENU_EVENT = "//li[@role='button']//span[text()='Event']";

        // File inputs (hidden inputs for upload)
        public const string IMAGE_VIDEO_INPUT = "input[accept='image/*,video/mp4,video/3gpp,video/quicktime']";
        public const string DOCUMENT_INPUT = "input[accept='*']";

        // Media preview and send (updated 2024)
        public const string MEDIA_CAPTION_INPUT = "p.selectable-text.copyable-text";
        public const string MEDIA_CAPTION_INPUT_ALT = "div[contenteditable='true'][data-tab='10']";
        public const string MEDIA_CAPTION_INPUT_ALT2 = "div[contenteditable='true'][role='textbox']";
        public const string MEDIA_SEND_BUTTON = "span[data-icon='send']";
        public const string MEDIA_SEND_BUTTON_ALT = "div[aria-label='Send']";
        public const string MEDIA_SEND_BUTTON_ALT2 = "button[aria-label='Send']";

        // Error indicators
        public const string INVALID_PHONE_ERROR = "div[data-animate-modal-body='true']";
        public const string POPUP_OK_BUTTON = "div[role='button']";
    }

    public WhatsAppService(IBrowserService browserService, IConfiguration configuration, ILogger<WhatsAppService> logger, IBulkOperationManager bulkManager, IServiceProvider serviceProvider)
    {
        _browserService = browserService;
        _configuration = configuration;
        _logger = logger;
        _bulkManager = bulkManager;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<StatusResultDto> GetStatusAsync()
    {
        var result = new StatusResultDto
        {
            BrowserType = _browserService.GetBrowserType()
        };

        if (!_browserService.IsBrowserOpen())
        {
            result.BrowserOpen = false;
            result.LoggedIn = false;
            result.Message = "Browser is not open. Call /init to start the browser.";
            return result;
        }

        result.BrowserOpen = true;

        try
        {
            var driver = _browserService.GetDriver();

            // Check if on WhatsApp Web
            if (!driver.Url.Contains("web.whatsapp.com"))
            {
                result.LoggedIn = false;
                result.Message = "Browser is open but not on WhatsApp Web.";
                return result;
            }

            // Check if logged in (side panel visible means logged in)
            var isLoggedIn = await CheckIfLoggedInAsync(driver);
            result.LoggedIn = isLoggedIn;
            result.Message = isLoggedIn
                ? "WhatsApp is ready. You can send messages."
                : "WhatsApp Web is open. Please scan the QR code to login.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking WhatsApp status");
            result.Message = $"Error checking status: {ex.Message}";
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<StatusResultDto> InitializeAsync(string? browserType = null)
    {
        var result = new StatusResultDto();

        try
        {
            _browserService.InitializeBrowser(browserType);
            result.BrowserType = _browserService.GetBrowserType();
            result.BrowserOpen = true;

            var driver = _browserService.GetDriver();

            // Navigate to WhatsApp Web
            _logger.LogInformation("Navigating to WhatsApp Web");
            driver.Navigate().GoToUrl(WHATSAPP_URL);

            // Wait for page to load
            await Task.Delay(3000);

            // Check if already logged in
            var isLoggedIn = await CheckIfLoggedInAsync(driver);
            result.LoggedIn = isLoggedIn;

            if (isLoggedIn)
            {
                result.Message = "WhatsApp is ready. You can send messages.";
                _logger.LogInformation("WhatsApp is already logged in");
            }
            else
            {
                result.Message = "WhatsApp Web is open. Please scan the QR code to login.";
                _logger.LogInformation("Waiting for QR code scan");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing WhatsApp");
            result.BrowserOpen = _browserService.IsBrowserOpen();
            result.Message = $"Error initializing: {ex.Message}";
        }

        return result;
    }

    // Track last phone number to avoid re-navigation issues
    private string? _lastPhoneNumber;

    /// <inheritdoc />
    public async Task<SendResultDto> SendMessageAsync(SendMessageRequest request)
    {
        var result = new SendResultDto { Phone = request.Phone };

        try
        {
            var driver = _browserService.GetDriver();

            // Clean phone number
            var phone = CleanPhoneNumber(request.Phone);
            _logger.LogInformation("Sending message to {Phone}", phone);

            // Check if we're already on this contact's chat
            bool needsNavigation = true;
            if (_lastPhoneNumber == phone)
            {
                // Check if message input is already available
                var existingInputs = driver.FindElements(By.CssSelector(Selectors.MESSAGE_INPUT));
                if (existingInputs.Count > 0 && existingInputs[0].Displayed)
                {
                    _logger.LogInformation("Already on chat with {Phone}, skipping navigation", phone);
                    needsNavigation = false;
                }
            }

            if (needsNavigation)
            {
                // Navigate to chat using direct URL
                var chatUrl = $"{WHATSAPP_URL}/send?phone={phone}";
                _logger.LogInformation("Navigating to: {Url}", chatUrl);
                driver.Navigate().GoToUrl(chatUrl);

                // Wait for page to stabilize (reduced from 5000ms)
                await Task.Delay(2000);
            }

            // Wait for chat to load
            var waitTimeout = int.Parse(_configuration["BrowserSettings:ElementWaitTimeoutSeconds"] ?? "30");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitTimeout));

            // Try to find message input with retry
            bool chatLoaded = false;
            int retryCount = 0;
            const int maxRetries = 3;

            while (!chatLoaded && retryCount < maxRetries)
            {
                try
                {
                    wait.Until(d =>
                    {
                        var messageInputs = d.FindElements(By.CssSelector(Selectors.MESSAGE_INPUT));
                        if (messageInputs.Count > 0 && messageInputs[0].Displayed)
                        {
                            return true;
                        }
                        messageInputs = d.FindElements(By.CssSelector(Selectors.MESSAGE_INPUT_ALT));
                        if (messageInputs.Count > 0 && messageInputs[0].Displayed)
                        {
                            return true;
                        }
                        return false;
                    });
                    chatLoaded = true;
                }
                catch (WebDriverTimeoutException)
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        _logger.LogWarning("Chat not loaded, retry {Retry}/{Max}", retryCount, maxRetries);
                        // Try refreshing the page
                        driver.Navigate().Refresh();
                        await Task.Delay(3000);
                    }
                }
            }

            // If chat didn't load, check for error
            if (!chatLoaded)
            {
                if (await CheckForInvalidPhoneErrorAsync(driver))
                {
                    throw new InvalidOperationException($"Invalid phone number: {phone}");
                }
                throw new InvalidOperationException("Chat window did not load. The phone number may be invalid or not registered with WhatsApp.");
            }

            // Remember this phone number
            _lastPhoneNumber = phone;

            // Small delay for stability (reduced from 1500ms)
            await Task.Delay(500);

            // Check if we have attachments
            if (request.Attachments?.Any() == true)
            {
                // Send ALL attachments together with message as caption
                _logger.LogInformation("Sending {Count} attachments with caption: {Message}",
                    request.Attachments.Count, request.Message);

                await SendMultipleAttachmentsAsync(driver, request.Attachments.ToList(), request.Message);
                result.AttachmentsSent = request.Attachments.Count;
            }
            else if (!string.IsNullOrEmpty(request.Message))
            {
                // No attachments - send as text message only
                _logger.LogInformation("Sending text message (no attachments): {Message}", request.Message);
                await SendTextMessageAsync(driver, request.Message);
            }

            result.Success = true;
            _logger.LogInformation("Message sent successfully to {Phone}", phone);

            // Apply random delay after sending
            if (request.DelaySettings != null)
            {
                var delay = RandomDelayHelper.GetRandomSeconds(
                    request.DelaySettings.MinDelaySeconds,
                    request.DelaySettings.MaxDelaySeconds) - 30;
                _logger.LogInformation("Applying random delay of {Delay:F2} seconds", delay);
                await Task.Delay(TimeSpan.FromSeconds(delay));
                result.DelayAppliedSeconds = delay;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.LogError(ex, "Failed to send message to {Phone}", request.Phone);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<BulkResultDto> SendBulkAsync(SendBulkRequest request, CancellationToken cancellationToken = default)
    {
        var result = new BulkResultDto
        {
            TotalContacts = request.Contacts.Count
        };

        var startTime = DateTime.UtcNow;
        int messagesProcessedSinceLastBreak = 0;

        var delaySettings = request.DelaySettings ?? GetDefaultDelaySettings();
        var breakSettings = request.BreakSettings ?? GetDefaultBreakSettings();

        // Generate unpredictable break threshold
        int currentBreakThreshold = RandomDelayHelper.GetUnpredictableThreshold(
            breakSettings.MinBreakAfterMessages,
            breakSettings.MaxBreakAfterMessages
        );

        _logger.LogInformation("Starting bulk send to {Count} contacts. First break after {Threshold} messages",
            request.Contacts.Count, currentBreakThreshold);

        foreach (var contact in request.Contacts)
        {
            // Check cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Bulk send cancelled by user");
                break;
            }

            // Replace placeholders in message
            var personalizedMessage = ReplacePlaceholders(request.Message, contact);

            // Clone attachments - each contact gets fresh copies with NO caption
            // Caption will be set from personalized message in SendMessageAsync (first attachment only)
            var clonedAttachments = request.Attachments?.Select(a => new AttachmentDto
            {
                Base64 = a.Base64,
                FileName = a.FileName,
                MediaType = a.MediaType,
                Caption = null  // Explicitly null - will be set from message for first attachment
            }).ToList();

            // Create single message request
            var sendRequest = new SendMessageRequest
            {
                Phone = contact.Phone,
                Message = personalizedMessage,
                Attachments = clonedAttachments,
                DelaySettings = delaySettings
            };

            // Send message
            var sendResult = await SendMessageAsync(sendRequest);
            result.Results.Add(sendResult);

            // Count all processed messages
            messagesProcessedSinceLastBreak++;

            if (sendResult.Success)
            {
                result.Sent++;
            }
            else
            {
                result.Failed++;
            }

            // Check if break is needed
            if (breakSettings.Enabled &&
                messagesProcessedSinceLastBreak >= currentBreakThreshold)
            {
                var breakMinutes = RandomDelayHelper.GetRandomMinutes(
                    breakSettings.MinBreakMinutes,
                    breakSettings.MaxBreakMinutes);

                _logger.LogInformation(
                    "Taking break for {Minutes:F2} minutes after {Count} messages",
                    breakMinutes, messagesProcessedSinceLastBreak);

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(breakMinutes), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Break cancelled by user");
                    break;
                }

                result.BreaksTaken++;
                messagesProcessedSinceLastBreak = 0;

                // Generate new unpredictable threshold for next break
                currentBreakThreshold = RandomDelayHelper.GetUnpredictableThreshold(
                    breakSettings.MinBreakAfterMessages,
                    breakSettings.MaxBreakAfterMessages
                );
                _logger.LogInformation("Next break after {Threshold} messages", currentBreakThreshold);
            }
        }

        result.TotalTimeMinutes = Math.Round((DateTime.UtcNow - startTime).TotalMinutes, 2);

        _logger.LogInformation(
            "Bulk send completed: {Sent} sent, {Failed} failed, {Breaks} breaks, {Time} minutes",
            result.Sent, result.Failed, result.BreaksTaken, result.TotalTimeMinutes);

        return result;
    }

    /// <inheritdoc />
    public Task CloseAsync()
    {
        _browserService.CloseBrowser();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<BulkControlResponse> StartBulkAsync(SendBulkRequest request)
    {
        // Check if browser is open
        if (!_browserService.IsBrowserOpen())
        {
            return new BulkControlResponse
            {
                Success = false,
                Message = "Browser is not open. Call /init first to start the browser.",
                State = _bulkManager.GetState()
            };
        }

        // Check if logged in
        try
        {
            var driver = _browserService.GetDriver();
            var isLoggedIn = await CheckIfLoggedInAsync(driver);
            if (!isLoggedIn)
            {
                return new BulkControlResponse
                {
                    Success = false,
                    Message = "WhatsApp is not logged in. Please scan the QR code first.",
                    State = _bulkManager.GetState()
                };
            }
        }
        catch (Exception ex)
        {
            return new BulkControlResponse
            {
                Success = false,
                Message = $"Error checking WhatsApp status: {ex.Message}",
                State = _bulkManager.GetState()
            };
        }

        // Check if already running
        if (_bulkManager.IsRunning || _bulkManager.IsPaused)
        {
            return new BulkControlResponse
            {
                Success = false,
                Message = "A bulk operation is already in progress. Stop it first or wait for completion.",
                State = _bulkManager.GetState()
            };
        }

        // Generate operation ID
        var operationId = Guid.NewGuid().ToString("N")[..8];

        // Store request and start operation
        _bulkManager.StoredRequest = request;
        _bulkManager.StartOperation(operationId, request.Contacts.Count);

        // Run bulk send in background
        _ = Task.Run(async () => await ExecuteBulkSendAsync());

        return new BulkControlResponse
        {
            Success = true,
            Message = $"Bulk operation {operationId} started. Processing {request.Contacts.Count} contacts.",
            State = _bulkManager.GetState()
        };
    }

    /// <inheritdoc />
    public BulkControlResponse GetBulkStatus()
    {
        var state = _bulkManager.GetState();
        return new BulkControlResponse
        {
            Success = true,
            Message = state.Message,
            State = state
        };
    }

    /// <inheritdoc />
    public BulkControlResponse PauseBulk()
    {
        if (!_bulkManager.IsRunning)
        {
            return new BulkControlResponse
            {
                Success = false,
                Message = "No bulk operation is running.",
                State = _bulkManager.GetState()
            };
        }

        _bulkManager.Pause();
        return new BulkControlResponse
        {
            Success = true,
            Message = "Bulk operation paused. Call /resume to continue.",
            State = _bulkManager.GetState()
        };
    }

    /// <inheritdoc />
    public BulkControlResponse ResumeBulk()
    {
        if (!_bulkManager.IsPaused)
        {
            return new BulkControlResponse
            {
                Success = false,
                Message = "No paused bulk operation to resume.",
                State = _bulkManager.GetState()
            };
        }

        _bulkManager.Resume();
        return new BulkControlResponse
        {
            Success = true,
            Message = "Bulk operation resumed.",
            State = _bulkManager.GetState()
        };
    }

    /// <inheritdoc />
    public BulkControlResponse StopBulk()
    {
        var state = _bulkManager.GetState();
        if (state.Status == BulkOperationStatus.Idle || state.Status == BulkOperationStatus.Completed || state.Status == BulkOperationStatus.Stopped)
        {
            return new BulkControlResponse
            {
                Success = false,
                Message = "No active bulk operation to stop.",
                State = state
            };
        }

        _bulkManager.Stop();
        return new BulkControlResponse
        {
            Success = true,
            Message = "Bulk operation stopped.",
            State = _bulkManager.GetState()
        };
    }

    /// <summary>
    /// Execute bulk send with pause/stop support
    /// </summary>
    private async Task ExecuteBulkSendAsync()
    {
        var request = _bulkManager.StoredRequest;
        if (request == null) return;

        var delaySettings = request.DelaySettings ?? GetDefaultDelaySettings();
        var breakSettings = request.BreakSettings ?? GetDefaultBreakSettings();
        int messagesProcessedSinceLastBreak = 0;
        int sent = 0;
        int failed = 0;

        // Generate unpredictable break threshold for first cycle
        int currentBreakThreshold = RandomDelayHelper.GetUnpredictableThreshold(
            breakSettings.MinBreakAfterMessages,
            breakSettings.MaxBreakAfterMessages
        );

        // Log break settings for debugging
        _logger.LogInformation(
            "Break Settings - Enabled: {Enabled}, MinBreakAfterMessages: {MinBreak}, MaxBreakAfterMessages: {MaxBreak}, MinBreakMinutes: {MinMin}, MaxBreakMinutes: {MaxMin}",
            breakSettings.Enabled, breakSettings.MinBreakAfterMessages, breakSettings.MaxBreakAfterMessages,
            breakSettings.MinBreakMinutes, breakSettings.MaxBreakMinutes);
        _logger.LogInformation("First break will occur after {Threshold} messages", currentBreakThreshold);

        try
        {
            for (int i = _bulkManager.CurrentIndex; i < request.Contacts.Count; i++)
            {
                // Check for stop
                if (_bulkManager.IsStopped)
                {
                    _logger.LogInformation("Bulk send stopped at index {Index}", i);
                    break;
                }

                // Wait if paused
                await _bulkManager.WaitIfPausedAsync();

                // Check again after resume (might have been stopped while paused)
                if (_bulkManager.IsStopped)
                {
                    break;
                }

                _bulkManager.CurrentIndex = i;
                var contact = request.Contacts[i];

                // Pick message based on gender if both MaleMessage and FemaleMessage are provided
                string? messageToUse = request.Message;
                if (!string.IsNullOrEmpty(request.MaleMessage) && !string.IsNullOrEmpty(request.FemaleMessage))
                {
                    var gender = (contact.Gender ?? "").ToUpperInvariant();
                    messageToUse = gender == "F" ? request.FemaleMessage : request.MaleMessage;
                    _logger.LogDebug("Contact {Phone} gender={Gender}, using {MessageType} message",
                        contact.Phone, gender, gender == "F" ? "female" : "male");
                }
                else if (!string.IsNullOrEmpty(request.MaleMessage))
                {
                    messageToUse = request.MaleMessage;
                }
                else if (!string.IsNullOrEmpty(request.FemaleMessage))
                {
                    messageToUse = request.FemaleMessage;
                }

                // Replace placeholders in message
                var personalizedMessage = ReplacePlaceholders(messageToUse, contact);

                // Clone attachments - each contact gets fresh copies with NO caption
                // Caption will be set from personalized message in SendMessageAsync (first attachment only)
                var clonedAttachments = request.Attachments?.Select(a => new AttachmentDto
                {
                    Base64 = a.Base64,
                    FileName = a.FileName,
                    MediaType = a.MediaType,
                    Caption = null  // Explicitly null - will be set from message for first attachment
                }).ToList();

                // Create single message request
                var sendRequest = new SendMessageRequest
                {
                    Phone = contact.Phone,
                    Message = personalizedMessage,
                    Attachments = clonedAttachments,
                    DelaySettings = delaySettings
                };

                // Send message
                var sendResult = await SendMessageAsync(sendRequest);

                // Count ALL processed messages for break calculation
                messagesProcessedSinceLastBreak++;

                if (sendResult.Success)
                {
                    sent++;
                    // Update contact status to Delivered in database
                    await UpdateContactStatusAsync(contact.Id, contact.Phone, ContactStatus.Delivered, null);
                }
                else
                {
                    failed++;
                    // Update contact status to Failed in database
                    await UpdateContactStatusAsync(contact.Id, contact.Phone, ContactStatus.Failed, sendResult.Error);
                }

                // Update progress
                _bulkManager.UpdateProgress(i + 1, sent, failed, sendResult);

                // Update messages since break for frontend tracking
                _bulkManager.UpdateMessagesSinceBreak(messagesProcessedSinceLastBreak, currentBreakThreshold);

                _logger.LogInformation(
                    "Message {Index}/{Total} processed. Messages since last break: {Count}/{BreakThreshold}",
                    i + 1, request.Contacts.Count, messagesProcessedSinceLastBreak, currentBreakThreshold);

                // Check if break is needed
                bool shouldBreak = breakSettings.Enabled &&
                    messagesProcessedSinceLastBreak >= currentBreakThreshold &&
                    i < request.Contacts.Count - 1; // Don't break after last message

                if (shouldBreak)
                {
                    var breakMinutes = RandomDelayHelper.GetRandomMinutes(
                        breakSettings.MinBreakMinutes,
                        breakSettings.MaxBreakMinutes);

                    _logger.LogInformation(
                        "BREAK TRIGGERED! Taking break for {Minutes:F2} minutes after {Count} messages",
                        breakMinutes, messagesProcessedSinceLastBreak);

                    _bulkManager.IncrementBreaks();

                    // Start break tracking for frontend popup
                    _bulkManager.StartBreak(breakMinutes, i + 1, currentBreakThreshold);

                    // Break with pause/stop check
                    var breakEndTime = DateTime.UtcNow.AddMinutes(breakMinutes);
                    while (DateTime.UtcNow < breakEndTime)
                    {
                        if (_bulkManager.IsStopped) break;
                        await _bulkManager.WaitIfPausedAsync();
                        if (_bulkManager.IsStopped) break;
                        await Task.Delay(1000);
                    }

                    // Generate NEW unpredictable break threshold for next cycle
                    currentBreakThreshold = RandomDelayHelper.GetUnpredictableThreshold(
                        breakSettings.MinBreakAfterMessages,
                        breakSettings.MaxBreakAfterMessages
                    );


                    // End break tracking for frontend
                    _bulkManager.EndBreak();

                    _logger.LogInformation("Break completed. Resuming bulk send. Next break after {Threshold} messages", currentBreakThreshold);
                    messagesProcessedSinceLastBreak = 0;
                }
            }

            // Mark as completed if not stopped
            if (!_bulkManager.IsStopped)
            {
                _bulkManager.Complete();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk send execution");
            _bulkManager.Stop();
        }
    }

    /// <summary>
    /// Update contact status in database by contact ID
    /// </summary>
    private async Task UpdateContactStatusAsync(int? contactId, string phone, int status, string? errorMessage)
    {
        // Only update if contact ID is provided
        if (!contactId.HasValue)
        {
            _logger.LogDebug("No contact ID provided for phone {Phone}, skipping database update", phone);
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WhatsAppSaasContext>();

            // Get contact by ID first
            var contact = await dbContext.Contacts.FindAsync(contactId.Value);

            if (contact == null)
            {
                _logger.LogWarning("Contact not found in database for ID:{ContactId}", contactId.Value);
                return;
            }

            // Use UTC for PostgreSQL timestamp with time zone
            var now = DateTime.UtcNow;

            // Update status based on result (Sent or Failed)
            contact.Status = status;
            contact.LastStatusUpdateAt = now;
            contact.SendAttemptCount++;
            contact.LastAttempt = now;

            if (status == ContactStatus.Delivered)
            {
                contact.LastMessageSentAt = now;
                contact.IssueDescription = null;
                _logger.LogInformation("Contact ID:{ContactId} marked as DELIVERED", contactId.Value);
            }
            else if (status == ContactStatus.Failed)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    contact.IssueDescription = errorMessage.Length > 500
                        ? errorMessage[..500]
                        : errorMessage;
                }
                _logger.LogInformation("Contact ID:{ContactId} marked as FAILED. Error: {Error}", contactId.Value, errorMessage);
            }

            contact.UpdatedAt = now;

            await dbContext.SaveChangesAsync();
            _logger.LogDebug("Updated contact status for ID:{ContactId}: {Status}",
                contactId.Value, status == ContactStatus.Delivered ? "Delivered" : "Failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update contact status for ID:{ContactId}", contactId.Value);
        }
    }

    #region Private Methods

    private async Task<bool> CheckIfLoggedInAsync(IWebDriver driver)
    {
        try
        {
            await Task.Delay(2000);

            // Check if side panel exists (indicates logged in)
            var sidePanelElements = driver.FindElements(By.CssSelector(Selectors.SIDE_PANEL));
            if (sidePanelElements.Count > 0)
            {
                return true;
            }

            // Alternative: check for chat list
            var chatListElements = driver.FindElements(By.CssSelector(Selectors.CHAT_LIST));
            return chatListElements.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckForInvalidPhoneErrorAsync(IWebDriver driver)
    {
        try
        {
            await Task.Delay(2000);

            // Look for error popup with specific text
            var pageSource = driver.PageSource.ToLower();

            // Check for common invalid phone error messages
            var errorMessages = new[]
            {
                "phone number shared via url is invalid",
                "invalid phone",
                "couldn't find",
                "doesn't have whatsapp",
                "not on whatsapp"
            };

            bool hasError = errorMessages.Any(msg => pageSource.Contains(msg));

            if (hasError)
            {
                _logger.LogWarning("Invalid phone number detected in page");

                // Try to close any popup
                try
                {
                    var okButtons = driver.FindElements(By.CssSelector("div[role='button']"));
                    foreach (var button in okButtons)
                    {
                        try
                        {
                            var text = button.Text.ToLower();
                            if (text.Contains("ok") || text.Contains("close"))
                            {
                                button.Click();
                                await Task.Delay(500);
                                break;
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking for invalid phone");
            return false;
        }
    }

    private async Task SendTextMessageAsync(IWebDriver driver, string message)
    {
        // Find message input using multiple selectors
        IWebElement? messageInput = null;

        try
        {
            messageInput = driver.FindElement(By.CssSelector(Selectors.MESSAGE_INPUT));
        }
        catch
        {
            try
            {
                messageInput = driver.FindElement(By.CssSelector(Selectors.MESSAGE_INPUT_ALT));
            }
            catch
            {
                throw new InvalidOperationException("Could not find message input field");
            }
        }

        // Click to focus
        messageInput.Click();
        await Task.Delay(300);

        var js = (IJavaScriptExecutor)driver;

        // Step 1: Clear any existing content first
        js.ExecuteScript(@"
            var container = arguments[0];
            var pElement = container.querySelector('p.selectable-text');
            if (!pElement) pElement = container;
            pElement.focus();
            pElement.innerHTML = '';
            pElement.textContent = '';
        ", messageInput);

        await Task.Delay(200);

        // Step 2: Insert text using clipboard paste (works with Unicode/emoji)
        js.ExecuteScript(@"
            var container = arguments[0];
            var text = arguments[1];

            var pElement = container.querySelector('p.selectable-text');
            if (!pElement) pElement = container;

            pElement.focus();
            pElement.click();

            // Use DataTransfer to paste text
            var dataTransfer = new DataTransfer();
            dataTransfer.setData('text/plain', text);

            var pasteEvent = new ClipboardEvent('paste', {
                clipboardData: dataTransfer,
                bubbles: true,
                cancelable: true
            });

            pElement.dispatchEvent(pasteEvent);
        ", messageInput, message);

        // Wait for paste to complete
        await Task.Delay(500);

        // Find and click send button
        IWebElement? sendButton = null;
        try
        {
            sendButton = driver.FindElement(By.CssSelector(Selectors.SEND_BUTTON));
        }
        catch
        {
            try
            {
                sendButton = driver.FindElement(By.CssSelector(Selectors.SEND_BUTTON_ALT));
            }
            catch
            {
                throw new InvalidOperationException("Could not find send button");
            }
        }

        sendButton.Click();

        // Wait for message to be sent (reduced from 2000ms)
        await Task.Delay(1000);

        _logger.LogDebug("Text message sent");
    }

    /// <summary>
    /// Sends multiple attachments at once with a single caption (all in one WhatsApp message)
    /// </summary>
    private async Task SendMultipleAttachmentsAsync(IWebDriver driver, List<AttachmentDto> attachments, string? caption)
    {
        var tempFiles = new List<string>();

        try
        {
            // Save all attachments to temp files
            foreach (var attachment in attachments)
            {
                var tempPath = await FileHelper.SaveBase64ToTempFileAsync(attachment.Base64, attachment.FileName);
                tempFiles.Add(tempPath);
                _logger.LogDebug("Saved attachment to temp file: {Path}", tempPath);
            }

            // Click attachment button
            IWebElement? attachButton = null;
            var attachSelectors = new[]
            {
                Selectors.ATTACH_BUTTON,
                Selectors.ATTACH_BUTTON_ALT,
                Selectors.ATTACH_BUTTON_PLUS,
                Selectors.ATTACH_CLIP
            };

            foreach (var selector in attachSelectors)
            {
                try
                {
                    var elements = driver.FindElements(By.CssSelector(selector));
                    if (elements.Count > 0 && elements[0].Displayed)
                    {
                        attachButton = elements[0];
                        _logger.LogDebug("Found attach button with selector: {Selector}", selector);
                        break;
                    }
                }
                catch { }
            }

            if (attachButton == null)
            {
                throw new InvalidOperationException("Could not find attachment button");
            }

            attachButton.Click();
            await Task.Delay(1000); // Wait for menu to appear

            // Determine media type from first attachment (all should be same type ideally)
            var firstAttachment = attachments.First();

            // Click on the correct menu item based on media type
            IWebElement? menuItem = null;
            IWebElement fileInput;
            if (firstAttachment.MediaType.Equals("document", StringComparison.OrdinalIgnoreCase))
            {
                // Try to click Document menu item first
                menuItem = await FindAndClickMenuItemAsync(driver, "Document");
                await Task.Delay(500);
                fileInput = driver.FindElement(By.CssSelector(Selectors.DOCUMENT_INPUT));
            }
            else
            {
                // Try to click Photos & videos menu item first
                menuItem = await FindAndClickMenuItemAsync(driver, "Photos & videos");
                await Task.Delay(500);
                fileInput = driver.FindElement(By.CssSelector(Selectors.IMAGE_VIDEO_INPUT));
            }

            // Send ALL file paths at once (multiple files separated by newline)
            var allPaths = string.Join("\n", tempFiles);
            fileInput.SendKeys(allPaths);
            _logger.LogInformation("Uploaded {Count} files at once", tempFiles.Count);

            // Wait for upload preview (reduced from 4000 + 1000 per file)
            await Task.Delay(2000 + (500 * tempFiles.Count));

            // Add caption if provided
            if (!string.IsNullOrEmpty(caption))
            {
                _logger.LogInformation("Adding caption to attachments: {Caption}", caption);

                var js = (IJavaScriptExecutor)driver;
                IWebElement? captionInput = null;

                // Find caption input
                var captionSelectors = new[]
                {
                    "p.selectable-text.copyable-text.x15bjb6t",
                    "p.selectable-text.copyable-text",
                    "div[contenteditable='true'] p.selectable-text",
                    "div.copyable-area div[contenteditable='true']"
                };

                foreach (var selector in captionSelectors)
                {
                    try
                    {
                        var elements = driver.FindElements(By.CssSelector(selector));
                        foreach (var element in elements)
                        {
                            if (element.Displayed)
                            {
                                captionInput = element;
                                break;
                            }
                        }
                        if (captionInput != null) break;
                    }
                    catch { }
                }

                if (captionInput != null)
                {
                    captionInput.Click();
                    await Task.Delay(500);

                    // Use JavaScript for full Unicode/emoji support
                    js.ExecuteScript(@"
                        var element = arguments[0];
                        var text = arguments[1];
                        element.focus();
                        element.click();

                        // Clear existing content completely
                        element.innerHTML = '';
                        element.textContent = '';

                        var dataTransfer = new DataTransfer();
                        dataTransfer.setData('text/plain', text);
                        var pasteEvent = new ClipboardEvent('paste', {
                            clipboardData: dataTransfer,
                            bubbles: true,
                            cancelable: true
                        });
                        element.dispatchEvent(pasteEvent);

                        // Check if paste worked
                        var currentContent = element.textContent || '';
                        var pasteWorked = currentContent.trim().length > 0;

                        // If paste didn't work, clear and try insertText
                        if (!pasteWorked) {
                            element.innerHTML = '';
                            element.textContent = '';
                            document.execCommand('selectAll', false, null);
                            document.execCommand('delete', false, null);
                            document.execCommand('insertText', false, text);
                        }

                        // If still empty, set textContent directly
                        currentContent = element.textContent || '';
                        if (currentContent.trim().length === 0) {
                            element.textContent = text;
                            element.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true }));
                        }
                    ", captionInput, caption);

                    await Task.Delay(500);
                    _logger.LogInformation("Caption added successfully");
                }
            }

            // Click send button
            _logger.LogDebug("Looking for send button...");
            IWebElement? sendButton = null;
            var sendSelectors = new[]
            {
                Selectors.MEDIA_SEND_BUTTON,
                Selectors.MEDIA_SEND_BUTTON_ALT,
                Selectors.MEDIA_SEND_BUTTON_ALT2
            };

            foreach (var selector in sendSelectors)
            {
                try
                {
                    var elements = driver.FindElements(By.CssSelector(selector));
                    foreach (var element in elements)
                    {
                        if (element.Displayed)
                        {
                            sendButton = element;
                            _logger.LogDebug("Found send button with selector: {Selector}", selector);
                            break;
                        }
                    }
                    if (sendButton != null) break;
                }
                catch { }
            }

            if (sendButton == null)
            {
                throw new InvalidOperationException("Could not find media send button");
            }

            _logger.LogInformation("Clicking send button for {Count} attachments", attachments.Count);
            sendButton.Click();

            // Wait for send (reduced from 3000 + 1000 per file)
            await Task.Delay(1500 + (500 * tempFiles.Count));
            _logger.LogInformation("All {Count} attachments sent", attachments.Count);
        }
        finally
        {
            // Clean up temp files
            foreach (var tempFile in tempFiles)
            {
                FileHelper.DeleteTempFile(tempFile);
            }
        }
    }

    /// <summary>
    /// Finds and clicks on a menu item in the attachment dropdown menu by text
    /// </summary>
    private async Task<IWebElement?> FindAndClickMenuItemAsync(IWebDriver driver, string menuItemText)
    {
        IWebElement? menuItem = null;

        // Try multiple strategies to find the menu item
        var strategies = new List<Func<IWebElement?>>
        {
            // Strategy 1: XPath by exact text
            () => {
                try
                {
                    var xpath = $"//li[@role='button']//span[text()='{menuItemText}']";
                    var elements = driver.FindElements(By.XPath(xpath));
                    foreach (var el in elements)
                    {
                        if (el.Displayed)
                        {
                            // Click the parent li element
                            var parent = el.FindElement(By.XPath("./ancestor::li[@role='button']"));
                            return parent;
                        }
                    }
                    return null;
                }
                catch { return null; }
            },
            // Strategy 2: XPath contains text (for partial matches)
            () => {
                try
                {
                    var xpath = $"//li[@role='button']//span[contains(text(), '{menuItemText.Split(' ')[0]}')]";
                    var elements = driver.FindElements(By.XPath(xpath));
                    foreach (var el in elements)
                    {
                        if (el.Displayed)
                        {
                            var parent = el.FindElement(By.XPath("./ancestor::li[@role='button']"));
                            return parent;
                        }
                    }
                    return null;
                }
                catch { return null; }
            },
            // Strategy 3: Find all li[role='button'] and check text content
            () => {
                try
                {
                    var allMenuItems = driver.FindElements(By.CssSelector("li[role='button']"));
                    foreach (var item in allMenuItems)
                    {
                        if (item.Displayed && item.Text.Contains(menuItemText, StringComparison.OrdinalIgnoreCase))
                        {
                            return item;
                        }
                    }
                    return null;
                }
                catch { return null; }
            },
            // Strategy 4: CSS selector with data-animate-dropdown-item
            () => {
                try
                {
                    var items = driver.FindElements(By.CssSelector("li[data-animate-dropdown-item='true']"));
                    foreach (var item in items)
                    {
                        if (item.Displayed && item.Text.Contains(menuItemText, StringComparison.OrdinalIgnoreCase))
                        {
                            return item;
                        }
                    }
                    return null;
                }
                catch { return null; }
            }
        };

        foreach (var strategy in strategies)
        {
            menuItem = strategy();
            if (menuItem != null)
            {
                _logger.LogDebug("Found menu item '{MenuItemText}' using strategy", menuItemText);
                break;
            }
        }

        if (menuItem != null)
        {
            try
            {
                menuItem.Click();
                _logger.LogDebug("Clicked menu item: {MenuItemText}", menuItemText);
                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to click menu item {MenuItemText}: {Error}", menuItemText, ex.Message);
                // Try JavaScript click as fallback
                try
                {
                    var js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript("arguments[0].click();", menuItem);
                    _logger.LogDebug("Clicked menu item using JavaScript: {MenuItemText}", menuItemText);
                    await Task.Delay(300);
                }
                catch { }
            }
        }
        else
        {
            _logger.LogWarning("Could not find menu item: {MenuItemText}. Proceeding with direct file input.", menuItemText);
        }

        return menuItem;
    }

    private async Task SendAttachmentAsync(IWebDriver driver, AttachmentDto attachment)
    {
        string? tempFilePath = null;

        try
        {
            // Convert base64 to temp file
            tempFilePath = await FileHelper.SaveBase64ToTempFileAsync(attachment.Base64, attachment.FileName);
            _logger.LogDebug("Saved attachment to temp file: {Path}", tempFilePath);

            // Click attachment button (try multiple selectors)
            IWebElement? attachButton = null;
            var attachSelectors = new[]
            {
                Selectors.ATTACH_BUTTON,      // plus-rounded (new)
                Selectors.ATTACH_BUTTON_ALT,  // attach-menu-plus
                Selectors.ATTACH_BUTTON_PLUS, // div[title='Attach']
                Selectors.ATTACH_CLIP         // clip (legacy)
            };

            foreach (var selector in attachSelectors)
            {
                try
                {
                    var elements = driver.FindElements(By.CssSelector(selector));
                    if (elements.Count > 0 && elements[0].Displayed)
                    {
                        attachButton = elements[0];
                        _logger.LogDebug("Found attach button with selector: {Selector}", selector);
                        break;
                    }
                }
                catch { }
            }

            if (attachButton == null)
            {
                throw new InvalidOperationException("Could not find attachment button");
            }

            attachButton.Click();
            await Task.Delay(1000); // Wait for menu to appear

            // Click on the correct menu item based on media type
            IWebElement fileInput;
            if (attachment.MediaType.Equals("document", StringComparison.OrdinalIgnoreCase))
            {
                // Click Document menu item first
                await FindAndClickMenuItemAsync(driver, "Document");
                await Task.Delay(500);
                fileInput = driver.FindElement(By.CssSelector(Selectors.DOCUMENT_INPUT));
            }
            else
            {
                // Click Photos & videos menu item first
                await FindAndClickMenuItemAsync(driver, "Photos & videos");
                await Task.Delay(500);
                fileInput = driver.FindElement(By.CssSelector(Selectors.IMAGE_VIDEO_INPUT));
            }

            // Send file path to input (upload file)
            fileInput.SendKeys(tempFilePath);
            _logger.LogDebug("File path sent to input element");

            // Wait for upload preview (reduced from 4000ms)
            await Task.Delay(2000);

            // Add caption/message if provided
            if (!string.IsNullOrEmpty(attachment.Caption))
            {
                _logger.LogInformation("Adding caption to attachment: {Caption}", attachment.Caption);

                try
                {
                    var js = (IJavaScriptExecutor)driver;
                    IWebElement? captionInput = null;

                    // Method 1: Find the p.selectable-text.copyable-text element directly
                    var captionSelectors = new[]
                    {
                        "p.selectable-text.copyable-text.x15bjb6t",  // Exact class from WhatsApp
                        "p.selectable-text.copyable-text",           // Main caption input
                        "div[contenteditable='true'] p.selectable-text",  // P inside contenteditable
                        "div.copyable-area div[contenteditable='true']",  // Contenteditable in copyable-area
                        "footer div[contenteditable='true']",        // Footer input
                        "div[data-tab] div[contenteditable='true']"  // Tab area input
                    };

                    foreach (var selector in captionSelectors)
                    {
                        try
                        {
                            var elements = driver.FindElements(By.CssSelector(selector));
                            _logger.LogDebug("Selector '{Selector}' found {Count} elements", selector, elements.Count);

                            foreach (var element in elements)
                            {
                                if (element.Displayed)
                                {
                                    captionInput = element;
                                    _logger.LogInformation("Found caption input with selector: {Selector}", selector);
                                    break;
                                }
                            }
                            if (captionInput != null) break;
                        }
                        catch { }
                    }

                    // Fallback: Find parent contenteditable of the p element
                    if (captionInput == null)
                    {
                        try
                        {
                            var pElement = driver.FindElement(By.CssSelector("p.selectable-text.copyable-text"));
                            if (pElement != null && pElement.Displayed)
                            {
                                // Get the parent contenteditable div
                                captionInput = pElement.FindElement(By.XPath("./ancestor::div[@contenteditable='true']"));
                                _logger.LogInformation("Found caption via parent contenteditable");
                            }
                        }
                        catch { }
                    }

                    if (captionInput != null)
                    {
                        // Click to focus
                        captionInput.Click();
                        await Task.Delay(500);

                        // Use JavaScript with clipboard API for full Unicode/emoji support
                        _logger.LogInformation("Using clipboard paste for caption (full Unicode support)");

                        // Method: Use execCommand with DataTransfer for proper Unicode handling
                        var success = (bool)js.ExecuteScript(@"
                            var element = arguments[0];
                            var text = arguments[1];

                            // Focus the element
                            element.focus();
                            element.click();

                            // Method 1: Use document.execCommand with insertText
                            // Clear first
                            element.innerHTML = '';

                            // Create a DataTransfer object to simulate paste
                            var dataTransfer = new DataTransfer();
                            dataTransfer.setData('text/plain', text);

                            // Create paste event
                            var pasteEvent = new ClipboardEvent('paste', {
                                clipboardData: dataTransfer,
                                bubbles: true,
                                cancelable: true
                            });

                            // Try paste event first
                            var handled = element.dispatchEvent(pasteEvent);

                            // If paste didn't work, try insertText
                            if (element.textContent === '' || element.textContent.length < 5) {
                                document.execCommand('insertText', false, text);
                            }

                            // If still empty, set textContent directly
                            if (element.textContent === '' || element.textContent.length < 5) {
                                element.textContent = text;
                                element.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true, data: text }));
                            }

                            return element.textContent.length > 0;
                        ", captionInput, attachment.Caption);

                        await Task.Delay(500);
                        _logger.LogInformation("Caption inserted: success={Success}", success);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find caption input field, trying direct p element insertion");

                        // Last resort: Use JavaScript to find and set text directly
                        js.ExecuteScript(@"
                            var p = document.querySelector('p.selectable-text.copyable-text');
                            if (p) {
                                p.click();
                                p.focus();
                                p.textContent = arguments[0];
                                p.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true }));
                            }
                        ", attachment.Caption);
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not add caption to attachment");
                }
            }

            // Click send button (try multiple selectors)
            _logger.LogDebug("Looking for send button...");
            IWebElement? sendButton = null;
            var sendSelectors = new[]
            {
                Selectors.MEDIA_SEND_BUTTON,      // span[data-icon='send']
                Selectors.MEDIA_SEND_BUTTON_ALT,  // div[aria-label='Send']
                Selectors.MEDIA_SEND_BUTTON_ALT2  // button[aria-label='Send']
            };

            foreach (var selector in sendSelectors)
            {
                try
                {
                    var buttons = driver.FindElements(By.CssSelector(selector));
                    _logger.LogDebug("Selector {Selector} found {Count} elements", selector, buttons.Count);

                    if (buttons.Count > 0)
                    {
                        // Find visible and clickable send button
                        foreach (var btn in buttons)
                        {
                            try
                            {
                                if (btn.Displayed && btn.Enabled)
                                {
                                    sendButton = btn;
                                    _logger.LogDebug("Found clickable send button with selector: {Selector}", selector);
                                    break;
                                }
                            }
                            catch { }
                        }
                        if (sendButton != null) break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error with selector {Selector}", selector);
                }
            }

            if (sendButton != null)
            {
                _logger.LogInformation("Clicking send button for attachment");
                sendButton.Click();
                await Task.Delay(1000);
            }
            else
            {
                _logger.LogWarning("Could not find send button, trying Enter key");
                // Try pressing Enter as fallback
                try
                {
                    var activeElement = driver.SwitchTo().ActiveElement();
                    activeElement.SendKeys(Keys.Enter);
                }
                catch { }
            }

            // Wait for upload and send to complete (reduced from 5000ms)
            await Task.Delay(2000);

            _logger.LogInformation("Attachment sent: {FileName}", attachment.FileName);
        }
        finally
        {
            // Delete temp file
            if (tempFilePath != null)
            {
                FileHelper.DeleteTempFile(tempFilePath);
            }
        }
    }

    private string CleanPhoneNumber(string phone)
    {
        // Remove all non-numeric characters except leading +
        var cleaned = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Remove leading + if present (WhatsApp URL doesn't need it)
        if (cleaned.StartsWith('+'))
        {
            cleaned = cleaned[1..];
        }

        return cleaned;
    }

    private string ReplacePlaceholders(string? message, ContactDto contact)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        var firstName = contact.Name?.Split(' ').FirstOrDefault() ?? string.Empty;
        var arabicName = contact.ArabicName ?? contact.Name ?? string.Empty;
        var englishName = contact.EnglishName ?? contact.Name ?? string.Empty;

        _logger.LogInformation("ReplacePlaceholders - ContactId: {Id}, Phone: {Phone}, Name: '{Name}', ArabicName: '{ArabicName}', EnglishName: '{EnglishName}'",
            contact.Id, contact.Phone, contact.Name, contact.ArabicName, contact.EnglishName);

        var result = message;

        // Replace contact placeholders - support both {single} and {{double}} braces
        // Double braces first (to avoid partial replacement)
        result = result
            .Replace("{{name}}", contact.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{{phone}}", contact.Phone, StringComparison.OrdinalIgnoreCase)
            .Replace("{{firstName}}", firstName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{arabic_name}}", arabicName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{arabicName}}", arabicName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{english_name}}", englishName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{englishName}}", englishName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{English_Name}}", englishName, StringComparison.OrdinalIgnoreCase);

        // Replace Arabic variable names (with curly braces)
        result = result
            .Replace("{الاسم_بالعربي}", arabicName)
            .Replace("{الاسم_العربي}", arabicName)
            .Replace("{اسم_عربي}", arabicName)
            .Replace("{الاسم_انجليزي}", englishName)
            .Replace("{الاسم_بالانجليزي}", englishName)
            .Replace("{اسم_انجليزي}", englishName)
            .Replace("{English_Name}", englishName);

        // Replace random choice placeholders FIRST: {option1-option2-option3}
        // This must happen before single-brace variable replacement
        result = ReplaceRandomChoices(result);

        // Now replace single-brace variables (after random choices are processed)
        // Only replace if they look like variable names (no dashes inside)
        result = ReplaceSingleBraceVariables(result, contact, firstName, arabicName, englishName);

        // ALSO replace variables WITHOUT curly braces (for user convenience)
        // This handles cases where user types "arabic_name" instead of "{arabic_name}"
        result = result
            .Replace("arabic_name", arabicName, StringComparison.OrdinalIgnoreCase)
            .Replace("arabicname", arabicName, StringComparison.OrdinalIgnoreCase)
            .Replace("english_name", englishName, StringComparison.OrdinalIgnoreCase)
            .Replace("englishname", englishName, StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation("ReplacePlaceholders result for {Phone}: '{Result}'",
            contact.Phone, result.Length > 100 ? result.Substring(0, 100) + "..." : result);

        return result;
    }

    private string ReplaceSingleBraceVariables(string message, ContactDto contact, string firstName, string arabicName, string englishName)
    {
        // Replace single-brace variables that are NOT random choices (no dashes)
        // Pattern: {variable_name} where variable_name has no dashes
        var pattern = @"\{([a-zA-Z_]+)\}";

        return System.Text.RegularExpressions.Regex.Replace(message, pattern, match =>
        {
            var varName = match.Groups[1].Value.ToLowerInvariant();

            return varName switch
            {
                "name" => contact.Name ?? string.Empty,
                "phone" => contact.Phone,
                "firstname" => firstName,
                "arabic_name" => arabicName,
                "arabicname" => arabicName,
                "english_name" => englishName,
                "englishname" => englishName,
                _ => match.Value // Keep original if not recognized
            };
        }, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Replaces {option1-option2-option3} with a randomly selected option
    /// </summary>
    private string ReplaceRandomChoices(string message)
    {
        // Pattern: single curly braces containing options separated by -
        // Matches {text1-text2-text3} but not {{placeholder}}
        var pattern = @"(?<!\{)\{([^{}]+)\}(?!\})";

        return System.Text.RegularExpressions.Regex.Replace(message, pattern, match =>
        {
            var content = match.Groups[1].Value;

            // Split by - to get options
            var options = content.Split('-')
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrEmpty(o))
                .ToArray();

            if (options.Length == 0)
                return match.Value; // Return original if no valid options

            if (options.Length == 1)
                return options[0]; // Return the only option

            // Randomly select one option
            var selectedIndex = Random.Shared.Next(options.Length);
            var selected = options[selectedIndex];

            _logger.LogDebug("Random choice: selected '{Selected}' from options [{Options}]",
                selected, string.Join(", ", options));

            return selected;
        });
    }

    private DelaySettingsDto GetDefaultDelaySettings()
    {
        return new DelaySettingsDto
        {
            MinDelaySeconds = int.Parse(_configuration["DefaultDelaySettings:MinDelaySeconds"] ?? "30"),
            MaxDelaySeconds = int.Parse(_configuration["DefaultDelaySettings:MaxDelaySeconds"] ?? "60")
        };
    }

    private BreakSettingsDto GetDefaultBreakSettings()
    {
        return new BreakSettingsDto
        {
            Enabled = bool.Parse(_configuration["DefaultBreakSettings:Enabled"] ?? "true"),
            MinBreakAfterMessages = int.Parse(_configuration["DefaultBreakSettings:MinBreakAfterMessages"] ?? "8"),
            MaxBreakAfterMessages = int.Parse(_configuration["DefaultBreakSettings:MaxBreakAfterMessages"] ?? "15"),
            MinBreakMinutes = int.Parse(_configuration["DefaultBreakSettings:MinBreakMinutes"] ?? "5"),
            MaxBreakMinutes = int.Parse(_configuration["DefaultBreakSettings:MaxBreakMinutes"] ?? "15")
        };
    }

    #endregion
}
