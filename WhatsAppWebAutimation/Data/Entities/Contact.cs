using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class Contact
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string ArabicName { get; set; } = null!;

    public string EnglishName { get; set; } = null!;

    public string FormattedPhone { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public int Status { get; set; }

    public string? IssueDescription { get; set; }

    public DateTime? LastMessageSentAt { get; set; }

    public DateTime? LastStatusUpdateAt { get; set; }

    public int SendAttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string ApiKeyId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Number { get; set; } = null!;

    public int? CampaignId { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentPath { get; set; }

    public string? AttachmentType { get; set; }

    public long? AttachmentSize { get; set; }

    public DateTime? AttachmentUploadedAt { get; set; }

    public string? PhoneNormalized { get; set; }

    public int? RowIndex { get; set; }

    public DateTime? LastAttempt { get; set; }

    public string? PhoneRaw { get; set; }

    public bool? IsSelected { get; set; }

    public int? OriginalRowIndex { get; set; }

    public string? City { get; set; }

    public string? Company { get; set; }

    public string? CustomField1 { get; set; }

    public string? CustomField2 { get; set; }

    public string? CustomField3 { get; set; }

    public virtual Campaign? Campaign { get; set; }

    public virtual ICollection<CampaignWorkflow> CampaignWorkflows { get; set; } = new List<CampaignWorkflow>();
}
