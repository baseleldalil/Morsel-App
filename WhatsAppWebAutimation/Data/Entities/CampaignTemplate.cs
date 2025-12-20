using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class CampaignTemplate
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string MessageTemplate { get; set; } = null!;

    public string? MaleContent { get; set; }

    public string? FemaleContent { get; set; }

    public bool AutoGenerateGenderVersions { get; set; }

    public string Category { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsGlobal { get; set; }

    public bool IsSystemTemplate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool HasAttachment { get; set; }

    public string? AttachmentPath { get; set; }

    public string? AttachmentName { get; set; }

    public string? AttachmentType { get; set; }

    public bool SupportsVariables { get; set; }

    public string? AvailableVariables { get; set; }

    public bool IsScheduled { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public string? ScheduleRecurrence { get; set; }

    public DateTime? LastScheduledRun { get; set; }

    public DateTime? NextScheduledRun { get; set; }

    public bool SupportsMultiLanguage { get; set; }

    public string? DefaultLanguage { get; set; }

    public string? TranslatedTemplates { get; set; }

    public int TimesUsed { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public decimal SuccessRate { get; set; }

    public string? TargetAudience { get; set; }

    public int? MaxRecipientsPerBatch { get; set; }

    public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();

    public virtual ICollection<MessageHistory> MessageHistories { get; set; } = new List<MessageHistory>();

    public virtual ICollection<TemplateAttachment> TemplateAttachments { get; set; } = new List<TemplateAttachment>();

    public virtual AspNetUser User { get; set; } = null!;
}
