using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class VideoTimingControl
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int MinDelayBeforeUploadSeconds { get; set; }

    public int MaxDelayBeforeUploadSeconds { get; set; }

    public int MinUploadTimeSeconds { get; set; }

    public int MaxUploadTimeSeconds { get; set; }

    public int MinDelayAfterUploadSeconds { get; set; }

    public int MaxDelayAfterUploadSeconds { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? SubscriptionPlanId { get; set; }

    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
}
