using System.ComponentModel.DataAnnotations;

namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Defines random delay rules for message sending
    /// Example: After 14 messages, pause for 4 minutes
    /// Uses irregular/broken numbers to simulate human behavior
    /// </summary>
    public class RandomDelayRule
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Rule Name")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 10000)]
        [Display(Name = "After Message Count")]
        public int AfterMessageCount { get; set; }

        [Range(1, 1440)]
        [Display(Name = "Pause Duration (minutes)")]
        public int PauseDurationMinutes { get; set; }

        [Range(0, 60)]
        [Display(Name = "Random Variance (seconds)")]
        [UIHint("RandomVariance")]
        public int RandomVarianceSeconds { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Priority")]
        [Range(1, 100)]
        public int Priority { get; set; } = 10;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Optional: Link to specific subscription (null = global default)
        public int? SubscriptionId { get; set; }
        public virtual Subscription? Subscription { get; set; }
    }
}
