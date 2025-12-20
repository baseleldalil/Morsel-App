using System.ComponentModel.DataAnnotations;

namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Campaign template model for predefined message templates
    /// Allows admins to create reusable message templates for users
    /// </summary>
    public class CampaignTemplate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string MessageContent { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? MaleContent { get; set; }

        [StringLength(2000)]
        public string? FemaleContent { get; set; }

        public bool AutoGenerateGenderVersions { get; set; } = true;

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Category { get; set; } = "General";

        [StringLength(10)]
        public string? Gender { get; set; }

        // Navigation properties
        public virtual ICollection<TemplateAttachment> Attachments { get; set; } = new List<TemplateAttachment>();
        public bool IsGlobal { get; internal set; }
        public double TimesUsed { get; internal set; }
    }
}