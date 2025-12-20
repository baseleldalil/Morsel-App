using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class ApiKey
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int SubscriptionPlanId { get; set; }

    public int DailyQuotaUsed { get; set; }

    public int TotalMessagesUsed { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime LastUsageResetAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
