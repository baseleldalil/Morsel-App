namespace WhatsAppWebAutomation.DTOs;

/// <summary>
/// Status of bulk operation
/// </summary>
public enum BulkOperationStatus
{
    Idle,
    Running,
    Paused,
    Stopped,
    Completed
}

/// <summary>
/// Current state of bulk send operation
/// </summary>
public class BulkOperationState
{
    public string OperationId { get; set; } = string.Empty;
    public BulkOperationStatus Status { get; set; } = BulkOperationStatus.Idle;
    public int TotalContacts { get; set; }
    public int ProcessedContacts { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int BreaksTaken { get; set; }

    // Break tracking
    public bool IsOnBreak { get; set; }
    public DateTime? BreakStartedAt { get; set; }
    public DateTime? BreakEndsAt { get; set; }
    public int BreakTriggeredAtMessage { get; set; }
    public int NextBreakAfterMessages { get; set; }
    public int MessagesSinceLastBreak { get; set; }
    public double BreakDurationMinutes { get; set; }

    public int RemainingContacts => TotalContacts - ProcessedContacts;
    public double ProgressPercent => TotalContacts > 0 ? Math.Round((double)ProcessedContacts / TotalContacts * 100, 1) : 0;
    public DateTime? StartedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<SendResultDto> Results { get; set; } = new();
}

/// <summary>
/// Response for bulk operation control actions
/// </summary>
public class BulkControlResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public BulkOperationState? State { get; set; }
}
