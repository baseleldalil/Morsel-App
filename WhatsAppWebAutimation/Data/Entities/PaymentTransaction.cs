using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class PaymentTransaction
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int? SubscriptionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string? PaymentReference { get; set; }

    public string? PaymentGatewayResponse { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual UserSubscription? Subscription { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}
