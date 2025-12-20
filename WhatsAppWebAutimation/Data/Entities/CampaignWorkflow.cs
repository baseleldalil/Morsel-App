using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class CampaignWorkflow
{
    public int Id { get; set; }

    public int CampaignId { get; set; }

    public int ContactId { get; set; }

    public int WorkflowStatus { get; set; }

    public DateTime AddedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? OpenedAt { get; set; }

    public DateTime? ClickedAt { get; set; }

    public string? MaleMessage { get; set; }

    public string? FemaleMessage { get; set; }

    public string? AttachmentBase64 { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentContentType { get; set; }

    public long? AttachmentSize { get; set; }

    public string? AttachmentType { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;

    public virtual Contact Contact { get; set; } = null!;
}
