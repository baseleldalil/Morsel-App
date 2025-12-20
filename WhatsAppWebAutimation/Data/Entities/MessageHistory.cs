using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class MessageHistory
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string MessageContent { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public string? ErrorMessage { get; set; }

    public bool HasAttachment { get; set; }

    public string? AttachmentName { get; set; }

    public string? AttachmentType { get; set; }

    public long? AttachmentSize { get; set; }

    public string? ApiKeyUsed { get; set; }

    public int? CampaignId { get; set; }

    public decimal? Cost { get; set; }

    public int? CampaignId1 { get; set; }

    public virtual CampaignTemplate? Campaign { get; set; }

    public virtual Campaign? CampaignId1Navigation { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}
