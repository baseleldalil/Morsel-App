using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class MessageTimingControl
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int MinDelaySeconds { get; set; }

    public int MaxDelaySeconds { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? SubscriptionPlanId { get; set; }

    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
}
