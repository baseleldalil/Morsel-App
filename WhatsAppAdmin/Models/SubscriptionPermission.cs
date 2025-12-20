namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Junction table linking subscriptions to their assigned permissions
    /// Enables many-to-many relationship between subscriptions and permissions
    /// </summary>
    public class SubscriptionPermission
    {
        public int SubscriptionId { get; set; }
        public int PermissionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Subscription Subscription { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}