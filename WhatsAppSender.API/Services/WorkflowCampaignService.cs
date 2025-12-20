using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppSender.API.Models;

namespace WhatsAppSender.API.Services
{
    public interface IWorkflowCampaignService
    {
        Task<StartWorkflowCampaignResponse> StartCampaignAsync(int campaignId, string userId, string browser, string timingMode);
        Task<WorkflowCampaignProgressResponse> GetProgressAsync(int campaignId, string userId);
        Task<StopWorkflowCampaignResponse> StopCampaignAsync(int campaignId, string userId);
        Task<PauseWorkflowCampaignResponse> PauseCampaignAsync(int campaignId, string userId);
        Task<ResumeWorkflowCampaignResponse> ResumeCampaignAsync(int campaignId, string userId, string browser);
    }

    public class WorkflowCampaignService : IWorkflowCampaignService
    {
        private readonly SaaSDbContext _context;
        private readonly IBrowserSessionManager _browserSessionManager;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<WorkflowCampaignService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Track active workflow executions
        private static readonly ConcurrentDictionary<int, WorkflowExecutionState> _activeWorkflows = new();
        private static readonly Random _random = new();

        public WorkflowCampaignService(
            SaaSDbContext context,
            IBrowserSessionManager browserSessionManager,
            IWhatsAppService whatsAppService,
            IApiKeyService apiKeyService,
            ILogger<WorkflowCampaignService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _browserSessionManager = browserSessionManager;
            _whatsAppService = whatsAppService;
            _apiKeyService = apiKeyService;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task<StartWorkflowCampaignResponse> StartCampaignAsync(int campaignId, string userId, string browser, string timingMode)
        {
            try
            {
                // Check if campaign exists and belongs to user
                var campaign = await _context.Campaigns
                    .FirstOrDefaultAsync(c => c.Id == campaignId && c.UserId == userId);

                if (campaign == null)
                {
                    return new StartWorkflowCampaignResponse
                    {
                        Success = false,
                        Message = "Campaign not found or access denied"
                    };
                }

                // Check if already running
                if (_activeWorkflows.ContainsKey(campaignId))
                {
                    return new StartWorkflowCampaignResponse
                    {
                        Success = false,
                        Message = "Campaign is already running",
                        CampaignId = campaignId,
                        Status = campaign.Status.ToString()
                    };
                }

                // Get workflow entries for this campaign
                var workflows = await _context.CampaignWorkflows
                    .Where(w => w.CampaignId == campaignId)
                    .ToListAsync();

                if (!workflows.Any())
                {
                    return new StartWorkflowCampaignResponse
                    {
                        Success = false,
                        Message = "No workflow entries found for this campaign"
                    };
                }

                // Get timing settings
                var timingSettings = await GetTimingSettingsAsync(userId, timingMode);

                // Create execution state
                var executionState = new WorkflowExecutionState
                {
                    CampaignId = campaignId,
                    UserId = userId,
                    Browser = browser,
                    TimingMode = timingMode,
                    StartedAt = DateTime.UtcNow,
                    NextBreakAfterMessages = GetRandomBreakPoint(timingSettings)
                };

                _activeWorkflows[campaignId] = executionState;

                // Update campaign status
                campaign.Status = CampaignStatus.Running;
                campaign.StartedAt = DateTime.UtcNow;
                campaign.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Start background processing
                _ = Task.Run(() => ProcessWorkflowAsync(campaignId, userId, browser, timingSettings, executionState));

                return new StartWorkflowCampaignResponse
                {
                    Success = true,
                    Message = "Campaign started successfully",
                    CampaignId = campaignId,
                    Status = "Running",
                    TotalContacts = workflows.Count,
                    Browser = browser,
                    TimingMode = timingMode,
                    TimingSettings = new TimingSettingsInfo
                    {
                        MinDelaySeconds = timingSettings.MinDelaySeconds,
                        MaxDelaySeconds = timingSettings.MaxDelaySeconds,
                        EnableRandomBreaks = timingSettings.EnableRandomBreaks,
                        MinMessagesBeforeBreak = timingSettings.MinMessagesBeforeBreak,
                        MaxMessagesBeforeBreak = timingSettings.MaxMessagesBeforeBreak,
                        MinBreakMinutes = timingSettings.MinBreakMinutes,
                        MaxBreakMinutes = timingSettings.MaxBreakMinutes
                    },
                    StartedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting workflow campaign {CampaignId}", campaignId);
                return new StartWorkflowCampaignResponse
                {
                    Success = false,
                    Message = $"Error starting campaign: {ex.Message}"
                };
            }
        }

        public async Task<WorkflowCampaignProgressResponse> GetProgressAsync(int campaignId, string userId)
        {
            var campaign = await _context.Campaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == campaignId && c.UserId == userId);

            if (campaign == null)
            {
                return new WorkflowCampaignProgressResponse
                {
                    CampaignId = campaignId,
                    Status = "NotFound"
                };
            }

            // Get workflow statistics
            var workflowStats = await _context.CampaignWorkflows
                .Where(w => w.CampaignId == campaignId)
                .GroupBy(w => w.WorkflowStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalContacts = workflowStats.Sum(s => s.Count);
            var pendingCount = workflowStats.FirstOrDefault(s => s.Status == WorkflowStatus.Pending)?.Count ?? 0;
            var processingCount = workflowStats.FirstOrDefault(s => s.Status == WorkflowStatus.Processing)?.Count ?? 0;
            var sentCount = workflowStats.FirstOrDefault(s => s.Status == WorkflowStatus.Sent)?.Count ?? 0;
            var deliveredCount = workflowStats.FirstOrDefault(s => s.Status == WorkflowStatus.Delivered)?.Count ?? 0;
            var failedCount = workflowStats.FirstOrDefault(s => s.Status == WorkflowStatus.Failed)?.Count ?? 0;
            var bouncedCount = workflowStats.FirstOrDefault(s => s.Status == WorkflowStatus.Bounced)?.Count ?? 0;

            var processedContacts = sentCount + deliveredCount + failedCount + bouncedCount;
            var progressPercentage = totalContacts > 0 ? (double)processedContacts / totalContacts * 100 : 0;

            // Get execution state if running
            _activeWorkflows.TryGetValue(campaignId, out var executionState);

            var response = new WorkflowCampaignProgressResponse
            {
                CampaignId = campaignId,
                CampaignName = campaign.Name,
                Status = campaign.Status.ToString(),
                TotalContacts = totalContacts,
                ProcessedContacts = processedContacts,
                PendingContacts = pendingCount + processingCount,
                ProgressPercentage = Math.Round(progressPercentage, 2),
                Statistics = new ContactStatistics
                {
                    Pending = pendingCount,
                    Processing = processingCount,
                    Sent = sentCount,
                    Delivered = deliveredCount,
                    Failed = failedCount,
                    Bounced = bouncedCount,
                    SuccessRate = processedContacts > 0 ? Math.Round((double)(sentCount + deliveredCount) / processedContacts * 100, 2) : 0
                },
                StartedAt = campaign.StartedAt,
                LastProcessedAt = executionState?.LastProcessedAt,
                MessagesSinceLastBreak = executionState?.MessagesSinceLastBreak ?? 0,
                IsOnBreak = executionState?.BreakEndTime.HasValue == true && executionState.BreakEndTime > DateTime.UtcNow,
                BreakRemainingSeconds = executionState?.BreakEndTime.HasValue == true && executionState.BreakEndTime > DateTime.UtcNow
                    ? (executionState.BreakEndTime.Value - DateTime.UtcNow).TotalSeconds
                    : null
            };

            // Estimate remaining time
            if (executionState != null && processedContacts > 0)
            {
                var elapsed = (DateTime.UtcNow - executionState.StartedAt).TotalMinutes;
                var avgTimePerContact = elapsed / processedContacts;
                var remainingContacts = totalContacts - processedContacts;
                response.EstimatedRemainingMinutes = Math.Round(avgTimePerContact * remainingContacts, 1);
            }

            return response;
        }

        public async Task<StopWorkflowCampaignResponse> StopCampaignAsync(int campaignId, string userId)
        {
            var campaign = await _context.Campaigns
                .FirstOrDefaultAsync(c => c.Id == campaignId && c.UserId == userId);

            if (campaign == null)
            {
                return new StopWorkflowCampaignResponse
                {
                    Success = false,
                    Message = "Campaign not found or access denied"
                };
            }

            // Stop execution
            if (_activeWorkflows.TryRemove(campaignId, out var executionState))
            {
                executionState.IsStopped = true;
                executionState.CancellationTokenSource.Cancel();
            }

            // Close browser
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    _browserSessionManager.CloseBrowser(user.Email ?? userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing browser for campaign {CampaignId}", campaignId);
            }

            // Update campaign status
            campaign.Status = CampaignStatus.Stopped;
            campaign.StoppedAt = DateTime.UtcNow;
            campaign.UpdatedAt = DateTime.UtcNow;

            // Get workflow counts
            var workflowStats = await _context.CampaignWorkflows
                .Where(w => w.CampaignId == campaignId)
                .GroupBy(w => w.WorkflowStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var processedCount = workflowStats
                .Where(s => s.Status != WorkflowStatus.Pending && s.Status != WorkflowStatus.Processing)
                .Sum(s => s.Count);
            var remainingCount = workflowStats
                .Where(s => s.Status == WorkflowStatus.Pending || s.Status == WorkflowStatus.Processing)
                .Sum(s => s.Count);

            await _context.SaveChangesAsync();

            return new StopWorkflowCampaignResponse
            {
                Success = true,
                Message = "Campaign stopped successfully",
                CampaignId = campaignId,
                Status = "Stopped",
                ProcessedContacts = processedCount,
                RemainingContacts = remainingCount,
                StoppedAt = DateTime.UtcNow
            };
        }

        public async Task<PauseWorkflowCampaignResponse> PauseCampaignAsync(int campaignId, string userId)
        {
            var campaign = await _context.Campaigns
                .FirstOrDefaultAsync(c => c.Id == campaignId && c.UserId == userId);

            if (campaign == null)
            {
                return new PauseWorkflowCampaignResponse
                {
                    Success = false,
                    Message = "Campaign not found or access denied"
                };
            }

            // Pause execution
            if (_activeWorkflows.TryGetValue(campaignId, out var executionState))
            {
                executionState.IsPaused = true;
            }

            // Update campaign status
            campaign.Status = CampaignStatus.Paused;
            campaign.PausedAt = DateTime.UtcNow;
            campaign.UpdatedAt = DateTime.UtcNow;

            // Get workflow counts
            var workflowStats = await _context.CampaignWorkflows
                .Where(w => w.CampaignId == campaignId)
                .GroupBy(w => w.WorkflowStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var processedCount = workflowStats
                .Where(s => s.Status != WorkflowStatus.Pending && s.Status != WorkflowStatus.Processing)
                .Sum(s => s.Count);
            var remainingCount = workflowStats
                .Where(s => s.Status == WorkflowStatus.Pending || s.Status == WorkflowStatus.Processing)
                .Sum(s => s.Count);

            await _context.SaveChangesAsync();

            return new PauseWorkflowCampaignResponse
            {
                Success = true,
                Message = "Campaign paused successfully",
                CampaignId = campaignId,
                Status = "Paused",
                ProcessedContacts = processedCount,
                RemainingContacts = remainingCount,
                PausedAt = DateTime.UtcNow
            };
        }

        public async Task<ResumeWorkflowCampaignResponse> ResumeCampaignAsync(int campaignId, string userId, string browser)
        {
            var campaign = await _context.Campaigns
                .FirstOrDefaultAsync(c => c.Id == campaignId && c.UserId == userId);

            if (campaign == null)
            {
                return new ResumeWorkflowCampaignResponse
                {
                    Success = false,
                    Message = "Campaign not found or access denied"
                };
            }

            if (campaign.Status != CampaignStatus.Paused)
            {
                return new ResumeWorkflowCampaignResponse
                {
                    Success = false,
                    Message = "Campaign is not paused"
                };
            }

            // Get timing settings (use manual mode on resume)
            var timingSettings = await GetTimingSettingsAsync(userId, "manual");

            // Resume or create new execution state
            WorkflowExecutionState executionState;
            if (_activeWorkflows.TryGetValue(campaignId, out var existingState))
            {
                existingState.IsPaused = false;
                executionState = existingState;
            }
            else
            {
                executionState = new WorkflowExecutionState
                {
                    CampaignId = campaignId,
                    UserId = userId,
                    Browser = browser,
                    TimingMode = "manual",
                    StartedAt = campaign.StartedAt ?? DateTime.UtcNow,
                    NextBreakAfterMessages = GetRandomBreakPoint(timingSettings)
                };
                _activeWorkflows[campaignId] = executionState;
            }

            // Update campaign status
            campaign.Status = CampaignStatus.Running;
            campaign.UpdatedAt = DateTime.UtcNow;

            // Get workflow counts
            var workflowStats = await _context.CampaignWorkflows
                .Where(w => w.CampaignId == campaignId)
                .GroupBy(w => w.WorkflowStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var processedCount = workflowStats
                .Where(s => s.Status != WorkflowStatus.Pending && s.Status != WorkflowStatus.Processing)
                .Sum(s => s.Count);
            var remainingCount = workflowStats
                .Where(s => s.Status == WorkflowStatus.Pending || s.Status == WorkflowStatus.Processing)
                .Sum(s => s.Count);

            await _context.SaveChangesAsync();

            // Resume background processing
            _ = Task.Run(() => ProcessWorkflowAsync(campaignId, userId, browser, timingSettings, executionState));

            return new ResumeWorkflowCampaignResponse
            {
                Success = true,
                Message = "Campaign resumed successfully",
                CampaignId = campaignId,
                Status = "Running",
                ProcessedContacts = processedCount,
                RemainingContacts = remainingCount,
                ResumedAt = DateTime.UtcNow
            };
        }

        #region Private Methods

        private async Task<AdvancedTimingSettings> GetTimingSettingsAsync(string userId, string timingMode)
        {
            AdvancedTimingSettings? settings = null;

            if (timingMode == "manual")
            {
                // Get user's timing settings
                settings = await _context.AdvancedTimingSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);
            }
            else // auto - use system admin settings
            {
                // Find system admin user
                var adminUser = await _context.Users
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                    .Join(_context.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r })
                    .Where(x => x.r.Name == "Admin" || x.r.Name == "SuperAdmin")
                    .Select(x => x.u)
                    .FirstOrDefaultAsync();

                if (adminUser != null)
                {
                    settings = await _context.AdvancedTimingSettings
                        .FirstOrDefaultAsync(s => s.UserId == adminUser.Id);
                }
            }

            // Return default settings if none found
            return settings ?? new AdvancedTimingSettings
            {
                MinDelaySeconds = 30.0,
                MaxDelaySeconds = 60.0,
                EnableRandomBreaks = true,
                MinMessagesBeforeBreak = 13,
                MaxMessagesBeforeBreak = 20,
                MinBreakMinutes = 4.0,
                MaxBreakMinutes = 9.0,
                UseDecimalRandomization = true,
                DecimalPrecision = 1
            };
        }

        private async Task ProcessWorkflowAsync(int campaignId, string userId, string browser, AdvancedTimingSettings timingSettings, WorkflowExecutionState executionState)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();
            var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

            try
            {
                // Get user's API key for sending
                var apiKey = await dbContext.ApiKeys
                    .Include(a => a.Subscription)
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive);

                if (apiKey == null)
                {
                    _logger.LogError("No active API key found for user {UserId}", userId);
                    await UpdateCampaignStatusAsync(dbContext, campaignId, CampaignStatus.Failed);
                    return;
                }

                // Create browser settings
                var browserSettings = new BrowserSettings
                {
                    Type = browser.ToLower() == "firefox" ? BrowserType.Firefox : BrowserType.Chrome,
                    KeepSessionOpen = true
                };

                // Process pending workflows
                while (!executionState.IsStopped && !executionState.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Check if paused
                    while (executionState.IsPaused && !executionState.IsStopped)
                    {
                        await Task.Delay(1000);
                    }

                    if (executionState.IsStopped) break;

                    // Check if on break
                    if (executionState.BreakEndTime.HasValue && DateTime.UtcNow < executionState.BreakEndTime.Value)
                    {
                        await Task.Delay(1000);
                        continue;
                    }

                    // Get next pending workflow
                    var workflow = await dbContext.CampaignWorkflows
                        .Include(w => w.Contact)
                        .Where(w => w.CampaignId == campaignId &&
                                   (w.WorkflowStatus == WorkflowStatus.Pending || w.WorkflowStatus == WorkflowStatus.New))
                        .OrderBy(w => w.AddedAt)
                        .FirstOrDefaultAsync();

                    if (workflow == null)
                    {
                        // No more pending contacts
                        _logger.LogInformation("Campaign {CampaignId} completed - no more pending contacts", campaignId);
                        await UpdateCampaignStatusAsync(dbContext, campaignId, CampaignStatus.Completed);
                        break;
                    }

                    // Update workflow status to processing
                    workflow.WorkflowStatus = WorkflowStatus.Processing;
                    await dbContext.SaveChangesAsync();

                    try
                    {
                        // Get contact
                        var contact = workflow.Contact ?? await dbContext.Contacts.FindAsync(workflow.ContactId);

                        if (contact == null)
                        {
                            workflow.WorkflowStatus = WorkflowStatus.Failed;
                            workflow.ErrorMessage = "Contact not found";
                            workflow.ProcessedAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync();
                            continue;
                        }

                        // Determine message based on gender
                        var message = contact.Gender?.ToUpper() == "M"
                            ? workflow.MaleMessage
                            : contact.Gender?.ToUpper() == "F"
                                ? workflow.FemaleMessage
                                : workflow.MaleMessage ?? workflow.FemaleMessage;

                        // Randomize message variables (format: {a|b|c|d})
                        if (!string.IsNullOrEmpty(message))
                        {
                            message = RandomizeMessageVariables(message);
                        }

                        // Get phone number
                        var phoneNumber = contact.PhoneNormalized ?? contact.FormattedPhone ?? contact.Number;
                        if (string.IsNullOrEmpty(phoneNumber))
                        {
                            workflow.WorkflowStatus = WorkflowStatus.Failed;
                            workflow.ErrorMessage = "No valid phone number";
                            workflow.ProcessedAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync();
                            continue;
                        }

                        // Clean phone number
                        phoneNumber = Regex.Replace(phoneNumber, @"[^\d+]", "");
                        if (!phoneNumber.StartsWith("+"))
                        {
                            phoneNumber = "+" + phoneNumber;
                        }

                        // Build WhatsAppMessage for the existing WhatsAppService
                        var whatsAppMessage = new WhatsAppMessage
                        {
                            Phone = phoneNumber.TrimStart('+'),
                            Messages = new List<string>()
                        };

                        // Add text message
                        if (!string.IsNullOrEmpty(message))
                        {
                            whatsAppMessage.Messages.Add(message);
                        }

                        // Add attachment if present
                        if (!string.IsNullOrEmpty(workflow.AttachmentBase64))
                        {
                            whatsAppMessage.Files.Add(new FileAttachment
                            {
                                FileBase64 = workflow.AttachmentBase64,
                                FileType = workflow.AttachmentContentType ?? "image/png",
                                FileName = workflow.AttachmentFileName ?? "attachment"
                            });
                        }

                        // Create request for WhatsAppService
                        var sendRequest = new SendMessageRequest
                        {
                            Messages = new List<WhatsAppMessage> { whatsAppMessage },
                            SendImmediately = true,
                            BrowserSettings = browserSettings,
                            TimingConfig = new TimingConfig
                            {
                                MinDelaySeconds = (int)timingSettings.MinDelaySeconds,
                                MaxDelaySeconds = (int)timingSettings.MaxDelaySeconds,
                                UseStrongRandomization = true
                            }
                        };

                        _logger.LogInformation("Sending message to {Phone} via WhatsAppService", phoneNumber);

                        // Use the existing WhatsAppService to send
                        var sendResult = await whatsAppService.SendMessagesAsync(sendRequest, apiKey);

                        // Update workflow status based on result
                        if (sendResult.Success && sendResult.ProcessedCount > 0)
                        {
                            workflow.WorkflowStatus = sendResult.DeliveredCount > 0 ? WorkflowStatus.Delivered : WorkflowStatus.Sent;
                            contact.Status = sendResult.DeliveredCount > 0 ? ContactStatus.Delivered : ContactStatus.Sent;
                            _logger.LogInformation("Message sent successfully to {Phone}", phoneNumber);
                        }
                        else
                        {
                            workflow.WorkflowStatus = WorkflowStatus.Failed;
                            workflow.ErrorMessage = sendResult.Message ?? string.Join(", ", sendResult.Errors);
                            contact.Status = ContactStatus.Failed;
                            _logger.LogWarning("Failed to send message to {Phone}: {Error}", phoneNumber, workflow.ErrorMessage);
                        }

                        workflow.ProcessedAt = DateTime.UtcNow;
                        contact.LastStatusUpdateAt = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();

                        // Update execution state
                        executionState.ProcessedCount++;
                        executionState.MessagesSinceLastBreak++;
                        executionState.LastProcessedAt = DateTime.UtcNow;

                        // Check if break is needed
                        if (timingSettings.EnableRandomBreaks &&
                            executionState.MessagesSinceLastBreak >= executionState.NextBreakAfterMessages)
                        {
                            var breakDuration = GetRandomBreakDuration(timingSettings);
                            executionState.BreakEndTime = DateTime.UtcNow.AddMinutes(breakDuration);
                            executionState.MessagesSinceLastBreak = 0;
                            executionState.NextBreakAfterMessages = GetRandomBreakPoint(timingSettings);

                            _logger.LogInformation("Campaign {CampaignId} taking break for {BreakMinutes} minutes",
                                campaignId, Math.Round(breakDuration, 1));
                        }
                        else
                        {
                            // Apply random delay
                            var delaySeconds = GetRandomDelay(timingSettings);
                            _logger.LogDebug("Waiting {DelaySeconds}s before next message", Math.Round(delaySeconds, 1));
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), executionState.CancellationTokenSource.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing workflow {WorkflowId}", workflow.Id);
                        workflow.WorkflowStatus = WorkflowStatus.Failed;
                        workflow.ErrorMessage = ex.Message;
                        workflow.ProcessedAt = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in workflow processing for campaign {CampaignId}", campaignId);
            }
            finally
            {
                _activeWorkflows.TryRemove(campaignId, out _);
            }
        }

        private string RandomizeMessageVariables(string message)
        {
            // Pattern: {option1|option2|option3}
            var pattern = @"\{([^}]+)\}";
            return Regex.Replace(message, pattern, match =>
            {
                var options = match.Groups[1].Value.Split('|');
                if (options.Length > 0)
                {
                    return options[_random.Next(options.Length)].Trim();
                }
                return match.Value;
            });
        }

        private double GetRandomDelay(AdvancedTimingSettings settings)
        {
            var delay = settings.MinDelaySeconds +
                       (_random.NextDouble() * (settings.MaxDelaySeconds - settings.MinDelaySeconds));

            if (settings.UseDecimalRandomization)
            {
                return Math.Round(delay, settings.DecimalPrecision);
            }
            return Math.Round(delay);
        }

        private double GetRandomBreakDuration(AdvancedTimingSettings settings)
        {
            var duration = settings.MinBreakMinutes +
                          (_random.NextDouble() * (settings.MaxBreakMinutes - settings.MinBreakMinutes));

            if (settings.UseDecimalRandomization)
            {
                return Math.Round(duration, settings.DecimalPrecision);
            }
            return Math.Round(duration);
        }

        private int GetRandomBreakPoint(AdvancedTimingSettings settings)
        {
            return _random.Next(settings.MinMessagesBeforeBreak, settings.MaxMessagesBeforeBreak + 1);
        }

        private async Task UpdateCampaignStatusAsync(SaaSDbContext dbContext, int campaignId, CampaignStatus status)
        {
            var campaign = await dbContext.Campaigns.FindAsync(campaignId);
            if (campaign != null)
            {
                campaign.Status = status;
                campaign.UpdatedAt = DateTime.UtcNow;
                if (status == CampaignStatus.Completed)
                {
                    campaign.CompletedAt = DateTime.UtcNow;
                }
                await dbContext.SaveChangesAsync();
            }
        }

        #endregion
    }
}
