using System.ComponentModel.DataAnnotations;

namespace WhatsAppSender.API.Models
{
    public class CreateCampaignRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? CampaignTemplateId { get; set; }

        /// <summary>
        /// General message content (used if not using gender templates)
        /// </summary>
        public string? MessageContent { get; set; }

        /// <summary>
        /// Use gender-specific templates
        /// </summary>
        public bool UseGenderTemplates { get; set; } = false;

        /// <summary>
        /// Message content for male contacts
        /// </summary>
        public string? MaleContent { get; set; }

        /// <summary>
        /// Message content for female contacts
        /// </summary>
        public string? FemaleContent { get; set; }

        /// <summary>
        /// Total number of contacts for this campaign
        /// </summary>
        public int TotalContacts { get; set; } = 0;
    }

    public class UpdateCampaignStatusRequest
    {
        [Required]
        public string Action { get; set; } = string.Empty; // "start", "pause", "stop"
    }

    public class CampaignResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int? CampaignTemplateId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalContacts { get; set; }
        public int MessagesSent { get; set; }
        public int MessagesDelivered { get; set; }
        public int MessagesFailed { get; set; }
        public int CurrentProgress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? LastError { get; set; }
        public int ErrorCount { get; set; }
        public string? MessageContent { get; set; }
        public bool UseGenderTemplates { get; set; }
        public string? MaleContent { get; set; }
        public string? FemaleContent { get; set; }
        public decimal ProgressPercentage { get; set; }
    }

    public class CampaignListResponse
    {
        public List<CampaignResponse> Campaigns { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class StartCampaignRequest
    {
        /// <summary>
        /// Timing mode: "auto" (from database) or "manual" (user-defined)
        /// </summary>
        public string TimingMode { get; set; } = "auto";

        /// <summary>
        /// Manual timing settings (only used if TimingMode is "manual")
        /// </summary>
        public ManualTimingDto? ManualTiming { get; set; }

        /// <summary>
        /// Browser type: "chrome" or "firefox"
        /// </summary>
        public string? BrowserType { get; set; } = "chrome";
    }

    public class ManualTimingDto
    {
        /// <summary>
        /// Minimum delay between messages in seconds
        /// </summary>
        [Range(1, 3600)]
        public int MinDelay { get; set; } = 30;

        /// <summary>
        /// Maximum delay between messages in seconds
        /// </summary>
        [Range(1, 3600)]
        public int MaxDelay { get; set; } = 60;
    }

    public class CampaignProgressRequest
    {
        /// <summary>
        /// Current progress index (last contact processed)
        /// </summary>
        public int CurrentProgress { get; set; } = 0;
    }
}
