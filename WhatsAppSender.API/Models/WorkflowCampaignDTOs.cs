using System.ComponentModel.DataAnnotations;

namespace WhatsAppSender.API.Models
{
    /// <summary>
    /// Request to start a workflow campaign
    /// </summary>
    public class StartWorkflowCampaignRequest
    {
        /// <summary>
        /// Campaign ID to start
        /// </summary>
        [Required]
        public int CampaignId { get; set; }

        /// <summary>
        /// Browser type: chrome or firefox
        /// </summary>
        [Required]
        [RegularExpression("^(chrome|firefox)$", ErrorMessage = "Browser must be 'chrome' or 'firefox'")]
        public string Browser { get; set; } = "chrome";

        /// <summary>
        /// Timing mode: manual (user settings) or auto (system admin settings)
        /// </summary>
        [Required]
        [RegularExpression("^(manual|auto)$", ErrorMessage = "TimingMode must be 'manual' or 'auto'")]
        public string TimingMode { get; set; } = "manual";
    }

    /// <summary>
    /// Response after starting workflow campaign
    /// </summary>
    public class StartWorkflowCampaignResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CampaignId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalContacts { get; set; }
        public string Browser { get; set; } = string.Empty;
        public string TimingMode { get; set; } = string.Empty;
        public TimingSettingsInfo? TimingSettings { get; set; }
        public DateTime StartedAt { get; set; }
    }

    /// <summary>
    /// Timing settings info
    /// </summary>
    public class TimingSettingsInfo
    {
        public double MinDelaySeconds { get; set; }
        public double MaxDelaySeconds { get; set; }
        public bool EnableRandomBreaks { get; set; }
        public int MinMessagesBeforeBreak { get; set; }
        public int MaxMessagesBeforeBreak { get; set; }
        public double MinBreakMinutes { get; set; }
        public double MaxBreakMinutes { get; set; }
    }

    /// <summary>
    /// Response for workflow campaign progress
    /// </summary>
    public class WorkflowCampaignProgressResponse
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Progress info
        public int TotalContacts { get; set; }
        public int ProcessedContacts { get; set; }
        public int PendingContacts { get; set; }
        public double ProgressPercentage { get; set; }

        // Contact statistics
        public ContactStatistics Statistics { get; set; } = new();

        // Timing info
        public DateTime? StartedAt { get; set; }
        public DateTime? LastProcessedAt { get; set; }
        public double? EstimatedRemainingMinutes { get; set; }

        // Current processing info
        public int? CurrentContactId { get; set; }
        public string? CurrentContactName { get; set; }
        public int MessagesSinceLastBreak { get; set; }
        public bool IsOnBreak { get; set; }
        public double? BreakRemainingSeconds { get; set; }
    }

    /// <summary>
    /// Contact statistics
    /// </summary>
    public class ContactStatistics
    {
        public int Pending { get; set; }
        public int Processing { get; set; }
        public int Sent { get; set; }
        public int Delivered { get; set; }
        public int Failed { get; set; }
        public int Bounced { get; set; }
        public int Blocked { get; set; }
        public int InvalidUrl { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Request to stop workflow campaign
    /// </summary>
    public class StopWorkflowCampaignRequest
    {
        [Required]
        public int CampaignId { get; set; }
    }

    /// <summary>
    /// Response for stop workflow campaign
    /// </summary>
    public class StopWorkflowCampaignResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CampaignId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProcessedContacts { get; set; }
        public int RemainingContacts { get; set; }
        public DateTime StoppedAt { get; set; }
    }

    /// <summary>
    /// Request to pause workflow campaign
    /// </summary>
    public class PauseWorkflowCampaignRequest
    {
        [Required]
        public int CampaignId { get; set; }
    }

    /// <summary>
    /// Response for pause workflow campaign
    /// </summary>
    public class PauseWorkflowCampaignResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CampaignId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProcessedContacts { get; set; }
        public int RemainingContacts { get; set; }
        public DateTime PausedAt { get; set; }
    }

    /// <summary>
    /// Request to resume workflow campaign
    /// </summary>
    public class ResumeWorkflowCampaignRequest
    {
        [Required]
        public int CampaignId { get; set; }

        /// <summary>
        /// Browser type: chrome or firefox
        /// </summary>
        [Required]
        [RegularExpression("^(chrome|firefox)$", ErrorMessage = "Browser must be 'chrome' or 'firefox'")]
        public string Browser { get; set; } = "chrome";
    }

    /// <summary>
    /// Response for resume workflow campaign
    /// </summary>
    public class ResumeWorkflowCampaignResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CampaignId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProcessedContacts { get; set; }
        public int RemainingContacts { get; set; }
        public DateTime ResumedAt { get; set; }
    }

    /// <summary>
    /// Internal model for workflow execution state
    /// </summary>
    public class WorkflowExecutionState
    {
        public int CampaignId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Browser { get; set; } = "chrome";
        public string TimingMode { get; set; } = "manual";
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
        public bool IsPaused { get; set; }
        public bool IsStopped { get; set; }
        public int MessagesSinceLastBreak { get; set; }
        public int NextBreakAfterMessages { get; set; }
        public DateTime? BreakEndTime { get; set; }
        public int ProcessedCount { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? LastProcessedAt { get; set; }
    }
}
