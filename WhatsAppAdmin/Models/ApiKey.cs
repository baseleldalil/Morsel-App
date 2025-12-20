
using System;
using System.ComponentModel.DataAnnotations;

namespace WhatsAppAdmin.Models
{
    public class ApiKey
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(64)]
        public string Key { get; set; }

        public int SubscriptionPlanId { get; set; }

        public int DailyQuotaUsed { get; set; }

        public DateTime LastUsedAt { get; set; }

        public DateTime LastResetAt { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public virtual AdminUser User { get; set; }
        public virtual Subscription SubscriptionPlan { get; set; }
    }
}
