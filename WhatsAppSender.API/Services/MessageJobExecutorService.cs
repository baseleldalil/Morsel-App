using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppSender.API.Models;
using System.Collections.Concurrent;

namespace WhatsAppSender.API.Services
{
    public interface IMessageJobExecutorService
    {
        Task<string> StartMessageJobAsync(SendMessageRequest request, string apiKey, TimingMode timingMode, ManualTimingSettings? manualSettings = null);
        Task<bool> PauseJobAsync(string jobId);
        Task<bool> ResumeJobAsync(string jobId);
        Task<bool> StopJobAsync(string jobId);
        MessageJobStatus? GetJobStatus(string jobId);
        List<MessageJobStatus> GetAllJobs(string userId);
    }

    public class MessageJobExecutorService : IMessageJobExecutorService
    {
        private readonly ILogger<MessageJobExecutorService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IBrowserSessionManager _browserSessionManager;
        private static readonly ConcurrentDictionary<string, MessageJobContext> _runningJobs = new();

        public MessageJobExecutorService(
            ILogger<MessageJobExecutorService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IWhatsAppService whatsAppService,
            IBrowserSessionManager browserSessionManager)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _whatsAppService = whatsAppService;
            _browserSessionManager = browserSessionManager;
        }

        public async Task<string> StartMessageJobAsync(SendMessageRequest request, string apiKey, TimingMode timingMode, ManualTimingSettings? manualSettings = null)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

                // Validate API key
                var apiKeyEntity = await apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    _logger.LogError("Invalid API key for message job");
                    throw new Exception("Invalid API key");
                }

                // Generate unique job ID
                string jobId = Guid.NewGuid().ToString();

                // Create job context
                var context = new MessageJobContext
                {
                    JobId = jobId,
                    ApiKey = apiKey,
                    UserId = apiKeyEntity.UserId,
                    UserEmail = apiKeyEntity.UserEmail,
                    Request = request,
                    TimingMode = timingMode,
                    ManualSettings = manualSettings,
                    CancellationTokenSource = new CancellationTokenSource(),
                    Status = MessageJobExecutionStatus.Running,
                    TotalMessages = request.Messages.Count,
                    StartedAt = DateTime.UtcNow
                };

                _runningJobs.TryAdd(jobId, context);

                // Start job execution in background
                _ = Task.Run(() => ExecuteMessageJobAsync(context), context.CancellationTokenSource.Token);

                _logger.LogInformation("Message job {JobId} started for user {UserEmail}", jobId, apiKeyEntity.UserEmail);
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting message job");
                throw;
            }
        }

        public async Task<bool> PauseJobAsync(string jobId)
        {
            if (!_runningJobs.TryGetValue(jobId, out var context))
            {
                _logger.LogWarning("Message job {JobId} not found", jobId);
                return false;
            }

            context.Status = MessageJobExecutionStatus.Paused;
            context.PausedAt = DateTime.UtcNow;

            _logger.LogInformation("Message job {JobId} paused", jobId);
            return true;
        }

        public async Task<bool> ResumeJobAsync(string jobId)
        {
            if (!_runningJobs.TryGetValue(jobId, out var context))
            {
                _logger.LogWarning("Message job {JobId} not found", jobId);
                return false;
            }

            if (context.Status != MessageJobExecutionStatus.Paused)
            {
                _logger.LogWarning("Message job {JobId} is not paused", jobId);
                return false;
            }

            context.Status = MessageJobExecutionStatus.Running;
            context.ResumedAt = DateTime.UtcNow;

            _logger.LogInformation("Message job {JobId} resumed", jobId);
            return true;
        }

        public async Task<bool> StopJobAsync(string jobId)
        {
            if (!_runningJobs.TryRemove(jobId, out var context))
            {
                _logger.LogWarning("Message job {JobId} not found", jobId);
                return false;
            }

            // Cancel the job execution
            context.CancellationTokenSource.Cancel();
            context.Status = MessageJobExecutionStatus.Stopped;
            context.StoppedAt = DateTime.UtcNow;

            // Close browser session
            try
            {
                _browserSessionManager.CloseBrowser(context.UserEmail);
                _logger.LogInformation("Browser session closed for job {JobId}", jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing browser session for job {JobId}", jobId);
            }

            _logger.LogInformation("Message job {JobId} stopped", jobId);
            return true;
        }

        public MessageJobStatus? GetJobStatus(string jobId)
        {
            if (_runningJobs.TryGetValue(jobId, out var context))
            {
                return new MessageJobStatus
                {
                    JobId = context.JobId,
                    Status = context.Status.ToString(),
                    TotalMessages = context.TotalMessages,
                    ProcessedMessages = context.ProcessedMessages,
                    SuccessCount = context.SuccessCount,
                    FailedCount = context.FailedCount,
                    CurrentPhone = context.CurrentPhone,
                    ProgressPercentage = context.TotalMessages > 0
                        ? (decimal)context.ProcessedMessages / context.TotalMessages * 100
                        : 0,
                    StartedAt = context.StartedAt,
                    PausedAt = context.PausedAt,
                    ResumedAt = context.ResumedAt,
                    StoppedAt = context.StoppedAt,
                    CompletedAt = context.CompletedAt,
                    LastError = context.LastError,
                    Results = context.Results
                };
            }
            return null;
        }

        public List<MessageJobStatus> GetAllJobs(string userId)
        {
            return _runningJobs.Values
                .Where(j => j.UserId == userId)
                .Select(context => new MessageJobStatus
                {
                    JobId = context.JobId,
                    Status = context.Status.ToString(),
                    TotalMessages = context.TotalMessages,
                    ProcessedMessages = context.ProcessedMessages,
                    SuccessCount = context.SuccessCount,
                    FailedCount = context.FailedCount,
                    CurrentPhone = context.CurrentPhone,
                    ProgressPercentage = context.TotalMessages > 0
                        ? (decimal)context.ProcessedMessages / context.TotalMessages * 100
                        : 0,
                    StartedAt = context.StartedAt,
                    PausedAt = context.PausedAt,
                    ResumedAt = context.ResumedAt,
                    StoppedAt = context.StoppedAt,
                    CompletedAt = context.CompletedAt,
                    LastError = context.LastError
                })
                .ToList();
        }

        private async Task ExecuteMessageJobAsync(MessageJobContext context)
        {
            try
            {
                _logger.LogInformation("Executing message job {JobId}", context.JobId);

                using var scope = _serviceScopeFactory.CreateScope();
                var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();

                var apiKeyEntity = await apiKeyService.ValidateApiKeyAsync(context.ApiKey);
                if (apiKeyEntity == null)
                {
                    _logger.LogError("API key validation failed for job {JobId}", context.JobId);
                    return;
                }

                // Get timing settings
                var timingSettings = await GetTimingSettingsAsync(context, dbContext);

                var random = new Random();

                for (int i = 0; i < context.Request.Messages.Count; i++)
                {
                    // Check if job is stopped
                    if (context.CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _logger.LogInformation("Message job {JobId} execution cancelled", context.JobId);
                        break;
                    }

                    // Wait if paused
                    while (context.Status == MessageJobExecutionStatus.Paused)
                    {
                        await Task.Delay(1000, context.CancellationTokenSource.Token);
                    }

                    var message = context.Request.Messages[i];
                    context.CurrentPhone = message.Phone;

                    try
                    {
                        _logger.LogInformation("Job {JobId}: Sending message {Current}/{Total} to {Phone}",
                            context.JobId, i + 1, context.TotalMessages, message.Phone);

                        // Send single message
                        var singleRequest = new SendMessageRequest
                        {
                            Messages = new List<WhatsAppMessage> { message },
                            TimingConfig = new TimingConfig
                            {
                                MinDelaySeconds = timingSettings.MinDelaySeconds,
                                MaxDelaySeconds = timingSettings.MaxDelaySeconds
                            },
                            BrowserSettings = context.Request.BrowserSettings,
                            SendImmediately = true
                        };

                        var result = await _whatsAppService.SendMessagesAsync(singleRequest, apiKeyEntity);

                        var messageResult = new MessageResult
                        {
                            Phone = message.Phone,
                            Success = result.Success,
                            Message = result.Message,
                            Error = result.Success ? null : string.Join(", ", result.Errors)
                        };

                        context.Results.Add(messageResult);

                        if (result.Success)
                        {
                            context.SuccessCount++;

                            // Log to message history
                            dbContext.MessageHistory.Add(new MessageHistory
                            {
                                UserId = context.UserId,
                                Phone = message.Phone,
                                MessageContent = message.Messages.FirstOrDefault() ?? "",
                                Status = "Sent",
                                SentAt = DateTime.UtcNow,
                                ApiKeyUsed = context.ApiKey
                            });
                        }
                        else
                        {
                            context.FailedCount++;
                            context.LastError = result.Message;

                            // Log to message history
                            dbContext.MessageHistory.Add(new MessageHistory
                            {
                                UserId = context.UserId,
                                Phone = message.Phone,
                                MessageContent = message.Messages.FirstOrDefault() ?? "",
                                Status = "Failed",
                                SentAt = DateTime.UtcNow,
                                ErrorMessage = result.Message,
                                ApiKeyUsed = context.ApiKey
                            });
                        }

                        context.ProcessedMessages++;
                        await dbContext.SaveChangesAsync();

                        // Apply delay before next message (except for last message)
                        if (i < context.Request.Messages.Count - 1)
                        {
                            int delaySeconds = random.Next(timingSettings.MinDelaySeconds, timingSettings.MaxDelaySeconds + 1);
                            int delayMs = delaySeconds * 1000;

                            _logger.LogInformation("Job {JobId}: Waiting {Delay} seconds before next message ({Next}/{Total})",
                                context.JobId, delaySeconds, i + 2, context.TotalMessages);

                            await Task.Delay(delayMs, context.CancellationTokenSource.Token);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Job {JobId}: Error sending message to {Phone}", context.JobId, message.Phone);

                        context.Results.Add(new MessageResult
                        {
                            Phone = message.Phone,
                            Success = false,
                            Message = "Failed",
                            Error = ex.Message
                        });
                        context.FailedCount++;
                        context.ProcessedMessages++;
                        context.LastError = ex.Message;
                    }
                }

                // Mark job as completed
                context.Status = MessageJobExecutionStatus.Completed;
                context.CompletedAt = DateTime.UtcNow;
                context.CurrentPhone = null;

                // Remove from running jobs after 5 minutes
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    _runningJobs.TryRemove(context.JobId, out _);
                });

                _logger.LogInformation("Message job {JobId} completed. Success: {Success}, Failed: {Failed}",
                    context.JobId, context.SuccessCount, context.FailedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error executing message job {JobId}", context.JobId);

                context.Status = MessageJobExecutionStatus.Failed;
                context.LastError = ex.Message;
                context.CompletedAt = DateTime.UtcNow;
            }
        }

        private async Task<TimingSettings> GetTimingSettingsAsync(MessageJobContext context, SaaSDbContext dbContext)
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
    }

    // Helper classes
    public class MessageJobContext
    {
        public string JobId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public SendMessageRequest Request { get; set; } = new();
        public TimingMode TimingMode { get; set; }
        public ManualTimingSettings? ManualSettings { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
        public MessageJobExecutionStatus Status { get; set; }
        public int TotalMessages { get; set; }
        public int ProcessedMessages { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public string? CurrentPhone { get; set; }
        public string? LastError { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime? ResumedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<MessageResult> Results { get; set; } = new();
    }

    public enum MessageJobExecutionStatus
    {
        Running,
        Paused,
        Stopped,
        Completed,
        Failed
    }

    public class MessageJobStatus
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int ProcessedMessages { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public string? CurrentPhone { get; set; }
        public decimal ProgressPercentage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime? ResumedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? LastError { get; set; }
        public List<MessageResult>? Results { get; set; }
    }

    public class TimingSettings
    {
        public int MinDelaySeconds { get; set; }
        public int MaxDelaySeconds { get; set; }
    }

  
    
}
