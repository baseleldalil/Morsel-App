using System.ComponentModel.DataAnnotations;

namespace WhatsApp.Shared.Models
{
    /// <summary>
    /// Controls base timing between messages
    /// Provides adjustable delays in seconds to prevent spam and respect rate limits
    /// </summary>
    public class MessageTimingControl
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Configuration Name")]
        public string Name { get; set; } = "Default Timing";

        [Range(0, 3600)]
        [Display(Name = "Minimum Delay (seconds)")]
        public int MinDelaySeconds { get; set; } = 1;

        [Range(0, 3600)]
        [Display(Name = "Maximum Delay (seconds)")]
        public int MaxDelaySeconds { get; set; } = 3;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Optional: Link to specific subscription (null = global default)
        public int? SubscriptionPlanId { get; set; }
        public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
    }
}
