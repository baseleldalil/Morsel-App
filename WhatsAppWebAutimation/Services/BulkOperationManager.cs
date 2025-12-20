using WhatsAppWebAutomation.DTOs;

namespace WhatsAppWebAutomation.Services;

/// <summary>
/// Interface for managing bulk operation state
/// </summary>
public interface IBulkOperationManager
{
    BulkOperationState GetState();
    void StartOperation(string operationId, int totalContacts);
    void UpdateProgress(int processed, int sent, int failed, SendResultDto result);
    void IncrementBreaks();
    void StartBreak(double durationMinutes, int triggeredAtMessage, int nextThreshold);
    void EndBreak();
    void UpdateMessagesSinceBreak(int count, int nextThreshold);
    void Pause();
    void Resume();
    void Stop();
    void Complete();
    void Reset();
    bool IsPaused { get; }
    bool IsStopped { get; }
    bool IsRunning { get; }
    Task WaitIfPausedAsync(CancellationToken cancellationToken = default);
    SendBulkRequest? StoredRequest { get; set; }
    int CurrentIndex { get; set; }
}

/// <summary>
/// Manages bulk operation state for pause/resume/stop functionality
/// </summary>
public class BulkOperationManager : IBulkOperationManager
{
    private readonly object _lock = new();
    private readonly ILogger<BulkOperationManager> _logger;
    private BulkOperationState _state = new();
    private readonly ManualResetEventSlim _pauseEvent = new(true); // true = not paused

    public BulkOperationManager(ILogger<BulkOperationManager> logger)
    {
        _logger = logger;
    }

    public bool IsPaused => _state.Status == BulkOperationStatus.Paused;
    public bool IsStopped => _state.Status == BulkOperationStatus.Stopped;
    public bool IsRunning => _state.Status == BulkOperationStatus.Running;
    public SendBulkRequest? StoredRequest { get; set; }
    public int CurrentIndex { get; set; }

    public BulkOperationState GetState()
    {
        lock (_lock)
        {
            return new BulkOperationState
            {
                OperationId = _state.OperationId,
                Status = _state.Status,
                TotalContacts = _state.TotalContacts,
                ProcessedContacts = _state.ProcessedContacts,
                Sent = _state.Sent,
                Failed = _state.Failed,
                BreaksTaken = _state.BreaksTaken,
                IsOnBreak = _state.IsOnBreak,
                BreakStartedAt = _state.BreakStartedAt,
                BreakEndsAt = _state.BreakEndsAt,
                BreakTriggeredAtMessage = _state.BreakTriggeredAtMessage,
                NextBreakAfterMessages = _state.NextBreakAfterMessages,
                MessagesSinceLastBreak = _state.MessagesSinceLastBreak,
                BreakDurationMinutes = _state.BreakDurationMinutes,
                StartedAt = _state.StartedAt,
                PausedAt = _state.PausedAt,
                CompletedAt = _state.CompletedAt,
                Message = _state.Message,
                Results = new List<SendResultDto>(_state.Results)
            };
        }
    }

    public void StartOperation(string operationId, int totalContacts)
    {
        lock (_lock)
        {
            _state = new BulkOperationState
            {
                OperationId = operationId,
                Status = BulkOperationStatus.Running,
                TotalContacts = totalContacts,
                StartedAt = DateTime.UtcNow,
                Message = "Bulk send operation started"
            };
            CurrentIndex = 0;
            _pauseEvent.Set(); // Ensure not paused
            _logger.LogInformation("Bulk operation {Id} started with {Count} contacts", operationId, totalContacts);
        }
    }

    public void UpdateProgress(int processed, int sent, int failed, SendResultDto result)
    {
        lock (_lock)
        {
            _state.ProcessedContacts = processed;
            _state.Sent = sent;
            _state.Failed = failed;
            _state.Results.Add(result);
            _state.Message = $"Processing: {processed}/{_state.TotalContacts} ({_state.ProgressPercent}%)";
        }
    }

    public void IncrementBreaks()
    {
        lock (_lock)
        {
            _state.BreaksTaken++;
        }
    }

    public void StartBreak(double durationMinutes, int triggeredAtMessage, int nextThreshold)
    {
        lock (_lock)
        {
            _state.IsOnBreak = true;
            _state.BreakStartedAt = DateTime.UtcNow;
            _state.BreakEndsAt = DateTime.UtcNow.AddMinutes(durationMinutes);
            _state.BreakTriggeredAtMessage = triggeredAtMessage;
            _state.BreakDurationMinutes = Math.Round(durationMinutes, 2);
            _state.NextBreakAfterMessages = nextThreshold;
            _state.Message = $"Taking break for {durationMinutes:F1} minutes after message {triggeredAtMessage}";
            _logger.LogInformation("Break started: {Duration} minutes, triggered at message {Message}, next break after {Next} messages",
                durationMinutes, triggeredAtMessage, nextThreshold);
        }
    }

    public void EndBreak()
    {
        lock (_lock)
        {
            _state.IsOnBreak = false;
            _state.BreakStartedAt = null;
            _state.BreakEndsAt = null;
            _state.MessagesSinceLastBreak = 0;
            _state.Message = $"Break completed. Resuming at message {_state.ProcessedContacts + 1}";
            _logger.LogInformation("Break ended. Next break after {Next} messages", _state.NextBreakAfterMessages);
        }
    }

    public void UpdateMessagesSinceBreak(int count, int nextThreshold)
    {
        lock (_lock)
        {
            _state.MessagesSinceLastBreak = count;
            _state.NextBreakAfterMessages = nextThreshold;
        }
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (_state.Status == BulkOperationStatus.Running)
            {
                _state.Status = BulkOperationStatus.Paused;
                _state.PausedAt = DateTime.UtcNow;
                _state.Message = $"Paused at {_state.ProcessedContacts}/{_state.TotalContacts}. Call /resume to continue.";
                _pauseEvent.Reset(); // Block the processing
                _logger.LogInformation("Bulk operation {Id} paused at {Index}", _state.OperationId, _state.ProcessedContacts);
            }
        }
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (_state.Status == BulkOperationStatus.Paused)
            {
                _state.Status = BulkOperationStatus.Running;
                _state.PausedAt = null;
                _state.Message = $"Resumed. Processing {_state.ProcessedContacts}/{_state.TotalContacts}";
                _pauseEvent.Set(); // Unblock the processing
                _logger.LogInformation("Bulk operation {Id} resumed", _state.OperationId);
            }
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (_state.Status == BulkOperationStatus.Running || _state.Status == BulkOperationStatus.Paused)
            {
                _state.Status = BulkOperationStatus.Stopped;
                _state.CompletedAt = DateTime.UtcNow;
                _state.Message = $"Stopped at {_state.ProcessedContacts}/{_state.TotalContacts}. Sent: {_state.Sent}, Failed: {_state.Failed}";
                _pauseEvent.Set(); // Unblock if paused so it can exit
                _logger.LogInformation("Bulk operation {Id} stopped", _state.OperationId);
            }
        }
    }

    public void Complete()
    {
        lock (_lock)
        {
            _state.Status = BulkOperationStatus.Completed;
            _state.CompletedAt = DateTime.UtcNow;
            _state.Message = $"Completed. Sent: {_state.Sent}, Failed: {_state.Failed}, Breaks: {_state.BreaksTaken}";
            _logger.LogInformation("Bulk operation {Id} completed", _state.OperationId);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state = new BulkOperationState();
            StoredRequest = null;
            CurrentIndex = 0;
            _pauseEvent.Set();
            _logger.LogInformation("Bulk operation state reset");
        }
    }

    public async Task WaitIfPausedAsync(CancellationToken cancellationToken = default)
    {
        // Wait if paused, but check periodically for stop signal
        while (!_pauseEvent.Wait(500))
        {
            if (cancellationToken.IsCancellationRequested || IsStopped)
            {
                return;
            }
        }
    }
}
