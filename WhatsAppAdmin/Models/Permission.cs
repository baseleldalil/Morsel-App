using System.ComponentModel.DataAnnotations;

namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Represents a permission that can be assigned to subscriptions
    /// Controls what features users can access based on their subscription
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SubscriptionPermission> SubscriptionPermissions { get; set; } = new List<SubscriptionPermission>();
    }
}