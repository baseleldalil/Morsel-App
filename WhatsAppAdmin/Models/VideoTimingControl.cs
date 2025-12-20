using System.ComponentModel.DataAnnotations;

namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Special timing controls for video messages
    /// Videos require longer upload times and different delay patterns
    /// </summary>
    public class VideoTimingControl
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Configuration Name")]
        public string Name { get; set; } = "Default Video Timing";

        [Range(0, 7200)]
        [Display(Name = "Minimum Delay Before Upload (seconds)")]
        public int MinDelayBeforeUploadSeconds { get; set; } = 5;

        [Range(0, 7200)]
        [Display(Name = "Maximum Delay Before Upload (seconds)")]
        public int MaxDelayBeforeUploadSeconds { get; set; } = 15;

        [Range(0, 7200)]
        [Display(Name = "Minimum Upload Time (seconds)")]
        public int MinUploadTimeSeconds { get; set; } = 10;

        [Range(0, 7200)]
        [Display(Name = "Maximum Upload Time (seconds)")]
        public int MaxUploadTimeSeconds { get; set; } = 30;

        [Range(0, 7200)]
        [Display(Name = "Minimum Delay After Upload (seconds)")]
        public int MinDelayAfterUploadSeconds { get; set; } = 3;

        [Range(0, 7200)]
        [Display(Name = "Maximum Delay After Upload (seconds)")]
        public int MaxDelayAfterUploadSeconds { get; set; } = 8;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Optional: Link to specific subscription (null = global default)
        public int? SubscriptionId { get; set; }
        public virtual Subscription? Subscription { get; set; }
    }
}
