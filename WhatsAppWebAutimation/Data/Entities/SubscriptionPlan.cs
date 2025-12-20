using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class SubscriptionPlan
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public int MaxMessagesPerDay { get; set; }

    public int MaxApiKeys { get; set; }

    public int MaxCampaignTemplates { get; set; }

    public int MaxContactsPerCampaign { get; set; }

    public bool HasPrioritySupport { get; set; }

    public bool HasCustomIntegrations { get; set; }

    public bool HasAdvancedAnalytics { get; set; }

    public bool HasScheduledMessages { get; set; }

    public bool HasTemplateVariables { get; set; }

    public bool HasMultiLanguageSupport { get; set; }

    public bool HasWhatsAppBotIntegration { get; set; }

    public bool HasCustomBranding { get; set; }

    public int TrialDurationDays { get; set; }

    public int BillingCycleDays { get; set; }

    public int GracePeriodDays { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public DateTime? DiscountValidUntil { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();

    public virtual ICollection<MessageTimingControl> MessageTimingControls { get; set; } = new List<MessageTimingControl>();

    public virtual ICollection<RandomDelayRule> RandomDelayRules { get; set; } = new List<RandomDelayRule>();

    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

    public virtual ICollection<VideoTimingControl> VideoTimingControls { get; set; } = new List<VideoTimingControl>();
}
