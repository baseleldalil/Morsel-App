using System.ComponentModel.DataAnnotations;

namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Subscription model defining user plans and limits
    /// Controls message quotas, permissions, and timer settings
    /// </summary>
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int MaxMessagesPerDay { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public int MaxApiKeys { get; set; }

        public bool HasPrioritySupport { get; set; }

        public bool HasCustomIntegrations { get; set; }

        public bool HasAdvancedAnalytics { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual TimerSettings? TimerSettings { get; set; }
        public virtual ICollection<SubscriptionPermission> SubscriptionPermissions { get; set; } = new List<SubscriptionPermission>();
        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }
}