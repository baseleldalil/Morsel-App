using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class Campaign
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string UserId { get; set; } = null!;

    public int? CampaignTemplateId { get; set; }

    public int Status { get; set; }

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

    public int? CurrentContactId { get; set; }

    public int? LastCompletedContactId { get; set; }

    public int? PausedAtContactId { get; set; }

    public string? DuplicatePreventionMode { get; set; }

    public string? SelectedBrowser { get; set; }

    public virtual CampaignTemplate? CampaignTemplate { get; set; }

    public virtual ICollection<CampaignWorkflow> CampaignWorkflows { get; set; } = new List<CampaignWorkflow>();

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    public virtual ICollection<MessageHistory> MessageHistories { get; set; } = new List<MessageHistory>();

    public virtual AspNetUser User { get; set; } = null!;
}
