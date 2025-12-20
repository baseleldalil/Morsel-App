using System.ComponentModel.DataAnnotations;

namespace WhatsAppSender.API.Models
{
    public class UserSubscription
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int SubscriptionId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Subscription? Subscription { get; set; }
    }

    public class AssignSubscriptionRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        public int SubscriptionId { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        [MinLength(6)]
        public string? Password { get; set; }
    }

    public class UserSubscriptionResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public int SubscriptionId { get; set; }
        public string SubscriptionName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
