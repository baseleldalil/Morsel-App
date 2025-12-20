using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class AspNetUser
{
    public string Id { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; }

    public string? CompanyName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public bool LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();

    public virtual ICollection<AspNetUserClaim> AspNetUserClaims { get; set; } = new List<AspNetUserClaim>();

    public virtual ICollection<AspNetUserLogin> AspNetUserLogins { get; set; } = new List<AspNetUserLogin>();

    public virtual ICollection<AspNetUserToken> AspNetUserTokens { get; set; } = new List<AspNetUserToken>();

    public virtual ICollection<CampaignTemplate> CampaignTemplates { get; set; } = new List<CampaignTemplate>();

    public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();

    public virtual ICollection<MessageHistory> MessageHistories { get; set; } = new List<MessageHistory>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<UsageStatistic> UsageStatistics { get; set; } = new List<UsageStatistic>();

    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

    public virtual ICollection<AspNetRole> Roles { get; set; } = new List<AspNetRole>();
}
