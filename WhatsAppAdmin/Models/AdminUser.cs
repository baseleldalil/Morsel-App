using Microsoft.AspNetCore.Identity;

namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Admin user model for authentication and authorization
    /// Extends IdentityUser to provide admin-specific functionality
    /// </summary>
    public class AdminUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }

        public virtual ICollection<ApiKey> ApiKeys { get; set; }
        public virtual ICollection<MessageHistory> MessageHistory { get; set; }
        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; }
    }
}