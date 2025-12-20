using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppSender.API.Models;
using System.Collections.Concurrent;

namespace WhatsAppSender.API.Services
{
    public interface ICampaignExecutorService
    {
        Task<bool> StartCampaignAsync(int campaignId, string apiKey, TimingMode timingMode, ManualTimingSettings? manualSettings = null, BrowserType browserType = BrowserType.Chrome);
        Task<bool> PauseCampaignAsync(int campaignId);
        Task<bool> StopCampaignAsync(int campaignId);
        Task<bool> ResumeCampaignAsync(int campaignId);
        CampaignExecutionStatus? GetCampaignStatus(int campaignId);
    }

    public class CampaignExecutorService : ICampaignExecutorService
    {
        private readonly ILogger<CampaignExecutorService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IBrowserSessionManager _browserSessionManager;
        private static readonly ConcurrentDictionary<int, CampaignExecutionContext> _runningCampaigns = new();

        public CampaignExecutorService(
            ILogger<CampaignExecutorService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IWhatsAppService whatsAppService,
            IBrowserSessionManager browserSessionManager)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _whatsAppService = whatsAppService;
            _browserSessionManager = browserSessionManager;
        }

        public async Task<bool> StartCampaignAsync(int campaignId, string apiKey, TimingMode timingMode, ManualTimingSettings? manualSettings = null, BrowserType browserType = BrowserType.Chrome)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();
                var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

                // Validate campaign
                var campaign = await dbContext.Campaigns
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == campaignId);

                if (campaign == null)
                {
                    _logger.LogError("Campaign {CampaignId} not found", campaignId);
                    return false;
                }

                // Validate API key
                var apiKeyEntity = await apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null || apiKeyEntity.UserId != campaign.UserId)
                {
                    _logger.LogError("Invalid API key or user mismatch for campaign {CampaignId}", campaignId);
                    return false;
                }

                // Check if campaign is already running
                if (_runningCampaigns.ContainsKey(campaignId))
                {
                    _logger.LogWarning("Campaign {CampaignId} is already running", campaignId);
                    return false;
                }

                // Update campaign status
                campaign.Status = CampaignStatus.Running;
                campaign.StartedAt = DateTime.UtcNow;
                campaign.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                // Create execution context
                var context = new CampaignExecutionContext
                {
                    CampaignId = campaignId,
                    ApiKey = apiKey,
                    TimingMode = timingMode,
                    ManualSettings = manualSettings,
                    BrowserType = browserType,
                    CancellationTokenSource = new CancellationTokenSource(),
                    Status = CampaignExecutionStatus.Running
                };

                _runningCampaigns.TryAdd(campaignId, context);

                // Start campaign execution in background
                _ = Task.Run(() => ExecuteCampaignAsync(context), context.CancellationTokenSource.Token);

                _logger.LogInformation("Campaign {CampaignId} started successfully", campaignId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting campaign {CampaignId}", campaignId);
                return false;
            }
        }

        public async Task<bool> PauseCampaignAsync(int campaignId)
        {
            if (!_runningCampaigns.TryGetValue(campaignId, out var context))
            {
                _logger.LogWarning("Campaign {CampaignId} is not running", campaignId);
                return false;
            }

            context.Status = CampaignExecutionStatus.Paused;

            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();

            var campaign = await dbContext.Campaigns.FindAsync(campaignId);
            if (campaign != null)
            {
                campaign.Status = CampaignStatus.Paused;
                campaign.PausedAt = DateTime.UtcNow;
                campaign.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            _logger.LogInformation("Campaign {CampaignId} paused", campaignId);
            return true;
        }

        public async Task<bool> StopCampaignAsync(int campaignId)
        {
            if (!_runningCampaigns.TryRemove(campaignId, out var context))
            {
                _logger.LogWarning("Campaign {CampaignId} is not running", campaignId);
                return false;
            }

            // Cancel the campaign execution
            context.CancellationTokenSource.Cancel();
            context.Status = CampaignExecutionStatus.Stopped;

            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();

            var campaign = await dbContext.Campaigns.FindAsync(campaignId);
            if (campaign != null)
            {
                campaign.Status = CampaignStatus.Stopped;
                campaign.StoppedAt = DateTime.UtcNow;
                campaign.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            // Close browser session for this campaign user
            try
            {
                var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
                var apiKeyEntity = await apiKeyService.ValidateApiKeyAsync(context.ApiKey);
                if (apiKeyEntity != null)
                {
                    _browserSessionManager.CloseBrowser(apiKeyEntity.UserEmail);
                    _logger.LogInformation("Browser session closed for campaign {CampaignId}", campaignId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing browser session for campaign {CampaignId}", campaignId);
            }

            _logger.LogInformation("Campaign {CampaignId} stopped", campaignId);
            return true;
        }

        public async Task<bool> ResumeCampaignAsync(int campaignId)
        {
            if (!_runningCampaigns.TryGetValue(campaignId, out var context))
            {
                _logger.LogWarning("Campaign {CampaignId} is not running", campaignId);
                return false;
            }

            if (context.Status != CampaignExecutionStatus.Paused)
            {
                _logger.LogWarning("Campaign {CampaignId} is not paused", campaignId);
                return false;
            }

            context.Status = CampaignExecutionStatus.Running;

            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();

            var campaign = await dbContext.Campaigns.FindAsync(campaignId);
            if (campaign != null)
            {
                campaign.Status = CampaignStatus.Running;
                campaign.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            _logger.LogInformation("Campaign {CampaignId} resumed", campaignId);
            return true;
        }

        public CampaignExecutionStatus? GetCampaignStatus(int campaignId)
        {
            if (_runningCampaigns.TryGetValue(campaignId, out var context))
            {
                return context.Status;
            }
            return null;
        }

        private async Task ExecuteCampaignAsync(CampaignExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Executing campaign {CampaignId}", context.CampaignId);

                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();
                var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

                // Get campaign and contacts
                var campaign = await dbContext.Campaigns
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == context.CampaignId);

                if (campaign == null)
                {
                    _logger.LogError("Campaign {CampaignId} not found during execution", context.CampaignId);
                    return;
                }

                var apiKeyEntity = await apiKeyService.ValidateApiKeyAsync(context.ApiKey);
                if (apiKeyEntity == null)
                {
                    _logger.LogError("API key validation failed for campaign {CampaignId}", context.CampaignId);
                    return;
                }

                // Get pending contacts for this campaign
                // IMPORTANT: Only include contacts with valid Gender (M or F) and IsSelected = true
                var contacts = await dbContext.Contacts
                    .Where(c => c.CampaignId == context.CampaignId
                        && c.Status == ContactStatus.Pending
                        && c.IsSelected == true
                        && (c.Gender == "M" || c.Gender == "F")) // Gender validation: must be M or F
                    .OrderBy(c => c.OriginalRowIndex) // Preserve original Excel row order
                    .Skip(campaign.CurrentProgress)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} contacts to process for campaign {CampaignId} (filtered by valid gender M/F and selected)", contacts.Count, context.CampaignId);

                // Mark contacts with invalid gender as NotValid
                var invalidGenderContacts = await dbContext.Contacts
                    .Where(c => c.CampaignId == context.CampaignId
                        && c.Status == ContactStatus.Pending
                        && c.Gender != "M" && c.Gender != "F")
                    .ToListAsync();

                if (invalidGenderContacts.Any())
                {
                    foreach (var contact in invalidGenderContacts)
                    {
                        contact.Status = ContactStatus.NotValid;
                        contact.IssueDescription = "Gender missing or invalid - must be Male (M) or Female (F)";
                        contact.UpdatedAt = DateTime.UtcNow;
                    }
                    await dbContext.SaveChangesAsync();
                    _logger.LogWarning("Marked {Count} contacts as NotValid due to invalid gender for campaign {CampaignId}",
                        invalidGenderContacts.Count, context.CampaignId);
                }

                // Get advanced timing settings for user
                var advancedTimingSettings = await dbContext.AdvancedTimingSettings
                    .FirstOrDefaultAsync(s => s.UserId == apiKeyEntity.UserId);

                // Create default if not exists
                if (advancedTimingSettings == null)
                {
                    advancedTimingSettings = new AdvancedTimingSettings
                    {
                        UserId = apiKeyEntity.UserId,
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
                }

                // Get timing service from service provider
                var timingService = scope.ServiceProvider.GetRequiredService<IAdvancedTimingService>();

                int messagesSent = 0;
                int messagesFailed = 0;
                int messagesSinceLastBreak = 0; // Track messages since last break

                foreach (var contact in contacts)
                {
                    // Check if campaign is stopped
                    if (context.CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _logger.LogInformation("Campaign {CampaignId} execution cancelled", context.CampaignId);
                        break;
                    }

                    // Wait if paused
                    while (context.Status == CampaignExecutionStatus.Paused)
                    {
                        await Task.Delay(1000, context.CancellationTokenSource.Token);
                    }

                    try
                    {
                        // Update contact status to sending
                        contact.Status = ContactStatus.Sending;
                        contact.UpdatedAt = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();

                        // Prepare message content
                        var messageContent = PrepareMessageContent(campaign, contact);

                        // Send message
                        var sendRequest = new SendMessageRequest
                        {
                            Messages = new List<WhatsAppMessage>
                            {
                                new WhatsAppMessage
                                {
                                    Phone = contact.FormattedPhone,
                                    Messages = new List<string> { messageContent }
                                }
                            },
                            BrowserSettings = new BrowserSettings
                            {
                                Type = context.BrowserType,
                                KeepSessionOpen = true
                            }
                        };

                        var sendResponse = await _whatsAppService.SendMessagesAsync(sendRequest, apiKeyEntity);

                        // Update contact based on result
                        if (sendResponse.Success)
                        {
                            contact.Status = ContactStatus.Sent;
                            contact.LastMessageSentAt = DateTime.UtcNow;
                            contact.SendAttemptCount++;
                            messagesSent++;

                            // Log to message history
                            dbContext.MessageHistory.Add(new MessageHistory
                            {
                                UserId = campaign.UserId,
                                Phone = contact.FormattedPhone,
                                MessageContent = messageContent,
                                Status = "Sent",
                                SentAt = DateTime.UtcNow,
                                CampaignId = campaign.Id,
                                ApiKeyUsed = context.ApiKey
                            });
                        }
                        else
                        {
                            contact.Status = ContactStatus.Failed;
                            contact.IssueDescription = sendResponse.Message;
                            contact.SendAttemptCount++;
                            messagesFailed++;

                            // Log to message history
                            dbContext.MessageHistory.Add(new MessageHistory
                            {
                                UserId = campaign.UserId,
                                Phone = contact.FormattedPhone,
                                MessageContent = messageContent,
                                Status = "Failed",
                                SentAt = DateTime.UtcNow,
                                ErrorMessage = sendResponse.Message,
                                CampaignId = campaign.Id,
                                ApiKeyUsed = context.ApiKey
                            });
                        }

                        contact.UpdatedAt = DateTime.UtcNow;
                        contact.LastStatusUpdateAt = DateTime.UtcNow;

                        // Update campaign progress
                        campaign.CurrentProgress++;
                        campaign.MessagesSent = messagesSent;
                        campaign.MessagesFailed = messagesFailed;
                        campaign.UpdatedAt = DateTime.UtcNow;

                        await dbContext.SaveChangesAsync();

                        // Increment message counter for break tracking
                        messagesSinceLastBreak++;

                        // Calculate timing for next message (includes break logic)
                        var timingInfo = timingService.CalculateNextMessageTiming(
                            advancedTimingSettings,
                            messagesSinceLastBreak
                        );

                        // Apply message delay (decimal seconds)
                        var delayMs = (int)(timingInfo.DelaySeconds * 1000);
                        _logger.LogInformation(
                            "⏱️ Campaign {CampaignId}: Waiting {Delay:F1}s before next message (Messages since break: {Count})",
                            context.CampaignId,
                            timingInfo.DelaySeconds,
                            messagesSinceLastBreak
                        );

                        await Task.Delay(delayMs, context.CancellationTokenSource.Token);

                        // Check if break is needed
                        if (timingInfo.IsBreakPoint && timingInfo.BreakDurationMinutes.HasValue)
                        {
                            var breakMs = (int)(timingInfo.BreakDurationMinutes.Value * 60 * 1000);
                            _logger.LogWarning(
                                "🛑 Campaign {CampaignId}: Taking break for {Duration:F1} minutes after {Count} messages",
                                context.CampaignId,
                                timingInfo.BreakDurationMinutes.Value,
                                messagesSinceLastBreak
                            );

                            // Reset counter
                            messagesSinceLastBreak = 0;

                            // Take break
                            await Task.Delay(breakMs, context.CancellationTokenSource.Token);

                            _logger.LogInformation(
                                "✅ Campaign {CampaignId}: Break completed, resuming campaign",
                                context.CampaignId
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending message to {Phone} in campaign {CampaignId}",
                            contact.FormattedPhone, context.CampaignId);

                        contact.Status = ContactStatus.Failed;
                        contact.IssueDescription = ex.Message;
                        contact.SendAttemptCount++;
                        messagesFailed++;

                        campaign.ErrorCount++;
                        campaign.LastError = ex.Message;
                        campaign.MessagesFailed = messagesFailed;

                        await dbContext.SaveChangesAsync();
                    }
                }

                // Mark campaign as completed
                campaign.Status = CampaignStatus.Completed;
                campaign.CompletedAt = DateTime.UtcNow;
                campaign.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                // Remove from running campaigns
                _runningCampaigns.TryRemove(context.CampaignId, out _);

                _logger.LogInformation("Campaign {CampaignId} completed. Sent: {Sent}, Failed: {Failed}",
                    context.CampaignId, messagesSent, messagesFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error executing campaign {CampaignId}", context.CampaignId);

                // Mark campaign as failed
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();

                var campaign = await dbContext.Campaigns.FindAsync(context.CampaignId);
                if (campaign != null)
                {
                    campaign.Status = CampaignStatus.Failed;
                    campaign.LastError = ex.Message;
                    campaign.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }

                _runningCampaigns.TryRemove(context.CampaignId, out _);
            }
        }

        private async Task<TimingSettings> GetTimingSettingsAsync(CampaignExecutionContext context, int apiKeyId, SaaSDbContext dbContext)
        {
            if (context.TimingMode == TimingMode.Manual && context.ManualSettings != null)
            {
                return new TimingSettings
                {
                    MinDelaySeconds = context.ManualSettings.MinDelay,
                    MaxDelaySeconds = context.ManualSettings.MaxDelay
                };
            }

            // Get from database (auto mode)
            // Get active timing configuration (global default)
            var activeTiming = await dbContext.MessageTimingControls
                .Where(mt => mt.IsActive && mt.SubscriptionPlanId == null)
                .OrderByDescending(mt => mt.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeTiming != null)
            {
                return new TimingSettings
                {
                    MinDelaySeconds = activeTiming.MinDelaySeconds,
                    MaxDelaySeconds = activeTiming.MaxDelaySeconds
                };
            }

            // Default fallback
            return new TimingSettings
            {
                MinDelaySeconds = 30,
                MaxDelaySeconds = 60
            };
        }

        private int GenerateRandomDelay(TimingSettings settings)
        {
            var random = new Random();
            var delaySeconds = random.Next(settings.MinDelaySeconds, settings.MaxDelaySeconds + 1);
            return delaySeconds * 1000; // Convert to milliseconds
        }

        private string PrepareMessageContent(Campaign campaign, Contact contact)
        {
            string message;

            // Determine which message to use based on gender templates
            if (campaign.UseGenderTemplates)
            {
                // Use gender-specific content
                if (contact.Gender == "M" && !string.IsNullOrEmpty(campaign.MaleContent))
                {
                    message = campaign.MaleContent;
                }
                else if (contact.Gender == "F" && !string.IsNullOrEmpty(campaign.FemaleContent))
                {
                    message = campaign.FemaleContent;
                }
                else
                {
                    // Fallback to general message content if gender-specific content is not available
                    message = campaign.MessageContent ?? string.Empty;
                }
            }
            else
            {
                // Use general message content
                message = campaign.MessageContent ?? string.Empty;
            }

            // Replace name placeholders if message is not empty
            if (!string.IsNullOrEmpty(message))
            {
                // Get the appropriate name for each placeholder
                // If ArabicName is empty, fall back to EnglishName, then FirstName
                var arabicNameValue = !string.IsNullOrWhiteSpace(contact.ArabicName)
                    ? contact.ArabicName
                    : (!string.IsNullOrWhiteSpace(contact.EnglishName) ? contact.EnglishName : contact.FirstName);

                // If EnglishName is empty, fall back to ArabicName, then FirstName
                var englishNameValue = !string.IsNullOrWhiteSpace(contact.EnglishName)
                    ? contact.EnglishName
                    : (!string.IsNullOrWhiteSpace(contact.ArabicName) ? contact.ArabicName : contact.FirstName);

                message = message
                    .Replace("{arabic_name}", arabicNameValue)
                    .Replace("{english_name}", englishNameValue)
                    .Replace("{first_name}", contact.FirstName)
                    .Replace("{phone}", contact.FormattedPhone);
            }

            return message;
        }
    }

    // Helper classes
    public class CampaignExecutionContext
    {
        public int CampaignId { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public TimingMode TimingMode { get; set; }
        public ManualTimingSettings? ManualSettings { get; set; }
        public BrowserType BrowserType { get; set; } = BrowserType.Chrome;
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
        public CampaignExecutionStatus Status { get; set; }
    }

    public enum CampaignExecutionStatus
    {
        Running,
        Paused,
        Stopped
    }

    public enum TimingMode
    {
        Auto,
        Manual
    }

    // Remove duplicate property declarations in ManualTimingSettings and TimingSettings classes

    public class ManualTimingSettings
    {
        public int MinDelay { get; set; }
        public int MaxDelay { get; set; }
    }
  
}
