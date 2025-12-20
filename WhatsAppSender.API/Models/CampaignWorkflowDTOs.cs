using System.ComponentModel.DataAnnotations;

namespace WhatsAppSender.API.Models
{
    /// <summary>
    /// Request to create a new campaign with workflow entries for specified contacts
    /// </summary>
    public class CreateCampaignWithWorkflowRequest
    {
        /// <summary>
        /// Name of the campaign
        /// </summary>
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Array of contact IDs to include in the campaign workflow
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one contact ID is required")]
        public int[] ContactIds { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Message content for male contacts
        /// </summary>
        public string? MaleMessage { get; set; }

        /// <summary>
        /// Message content for female contacts
        /// </summary>
        public string? FemaleMessage { get; set; }

        /// <summary>
        /// List of attachments with base64 data
        /// </summary>
        public List<AttachmentDto>? Attachments { get; set; }
    }

    /// <summary>
    /// Attachment data transfer object
    /// </summary>
    public class AttachmentDto
    {
        /// <summary>
        /// Original file name of the attachment
        /// </summary>
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the attachment (e.g., image/png, image/jpeg, video/mp4)
        /// </summary>
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Attachment data as Base64 encoded string
        /// </summary>
        [Required]
        public string Base64Data { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response returned after creating a campaign with workflow
    /// </summary>
    public class CreateCampaignWithWorkflowResponse
    {
        /// <summary>
        /// The generated campaign ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Campaign name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Campaign status (new, draft, scheduled, running, paused, completed, cancelled)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Number of contacts added to the workflow
        /// </summary>
        public int ContactsCount { get; set; }

        /// <summary>
        /// When the campaign was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Individual workflow entry response
    /// </summary>
    public class CampaignWorkflowResponse
    {
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public int ContactId { get; set; }
        public string WorkflowStatus { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }

        // Message content
        public string? MaleMessage { get; set; }
        public string? FemaleMessage { get; set; }

        // Attachment info (Base64 not included in response for performance)
        public bool HasAttachment { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentContentType { get; set; }
        public long? AttachmentSize { get; set; }
        public string? AttachmentType { get; set; }
    }

    /// <summary>
    /// Detailed workflow response including attachment base64 data
    /// </summary>
    public class CampaignWorkflowDetailResponse : CampaignWorkflowResponse
    {
        /// <summary>
        /// Base64 encoded attachment data
        /// </summary>
        public string? AttachmentBase64 { get; set; }
    }

    /// <summary>
    /// Request to update workflow status
    /// </summary>
    public class UpdateWorkflowStatusRequest
    {
        /// <summary>
        /// New workflow status: pending, processing, sent, delivered, failed, bounced, opened, clicked
        /// </summary>
        [Required]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Optional error message (for failed status)
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Campaign workflow summary with statistics
    /// </summary>
    public class CampaignWorkflowSummary
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public string CampaignStatus { get; set; } = string.Empty;
        public int TotalWorkflows { get; set; }
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int SentCount { get; set; }
        public int DeliveredCount { get; set; }
        public int FailedCount { get; set; }
        public int BouncedCount { get; set; }
        public int OpenedCount { get; set; }
        public int ClickedCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
