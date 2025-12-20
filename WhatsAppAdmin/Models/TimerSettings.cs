using System.ComponentModel.DataAnnotations;

namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Timer settings for controlling message sending delays
    /// Used to prevent spam and respect rate limits
    /// </summary>
    public class TimerSettings
    {
        public int Id { get; set; }

        [Range(0, int.MaxValue)]
        public int? MinDelaySeconds { get; set; }

        [Range(0, int.MaxValue)]
        public int? MaxDelaySeconds { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public int SubscriptionId { get; set; }
        public virtual Subscription Subscription { get; set; } = null!;
    }
}