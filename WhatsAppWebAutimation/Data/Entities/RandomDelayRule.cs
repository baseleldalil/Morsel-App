using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class RandomDelayRule
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int AfterMessageCount { get; set; }

    public int PauseDurationMinutes { get; set; }

    public int RandomVarianceSeconds { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public int Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? SubscriptionPlanId { get; set; }

    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
}
