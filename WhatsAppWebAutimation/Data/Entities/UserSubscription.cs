using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class UserSubscription
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int SubscriptionPlanId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal AmountPaid { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? PaymentReference { get; set; }

    public bool IsActive { get; set; }

    public bool AutoRenew { get; set; }

    public bool IsTrialPeriod { get; set; }

    public bool IsInGracePeriod { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? NextBillingDate { get; set; }

    public DateTime? TrialEndsAt { get; set; }

    public DateTime? GracePeriodEndsAt { get; set; }

    public int CurrentPeriodMessages { get; set; }

    public DateTime? LastUsageResetAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public string? CancelledBy { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
