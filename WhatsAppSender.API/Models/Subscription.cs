using System.ComponentModel.DataAnnotations;

namespace WhatsAppSender.API.Models
{
    /// <summary>
    /// Subscription model for WhatsApp API - simplified version for API key validation
    /// </summary>
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int MaxMessagesPerDay { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for API keys
        public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    }
}