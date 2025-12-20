using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class TemplateAttachment
{
    public int Id { get; set; }

    public int CampaignTemplateId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual CampaignTemplate CampaignTemplate { get; set; } = null!;
}
