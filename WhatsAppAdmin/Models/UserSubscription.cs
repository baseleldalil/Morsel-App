using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Links users to their assigned subscriptions
    /// Tracks subscription assignments and usage statistics
    /// </summary>
    public class UserSubscription
    {
        public int Id { get; set; }

        public string? UserId { get; set; } = null;

        [Required]
        [StringLength(200)]
        public string UserEmail { get; set; } = string.Empty;

        [NotMapped]
        [MinLength(6)]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public int SubscriptionId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Usage tracking
        public int MessagesUsedToday { get; set; } = 0;
        public DateTime LastMessageSentAt { get; set; } = DateTime.UtcNow;
        public DateTime LastResetAt { get; set; } = DateTime.UtcNow.Date;

        // Navigation properties
        public virtual Subscription Subscription { get; set; } = null!;
        public DateTime CreatedAt { get; internal set; }
        public decimal AmountPaid { get; internal set; }
    }
}