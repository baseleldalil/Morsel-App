using System.ComponentModel.DataAnnotations;

namespace WhatsAppSender.API.Models
{
    public class CampaignTemplate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // Gender-specific templates
        public string? MaleContent { get; set; }
        public string? FemaleContent { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // User-specific templates
        public string? UserId { get; set; }
        public User? User { get; set; }

        // System templates (UserId is null for system templates)
        public bool IsSystemTemplate { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateCampaignTemplateRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // Gender-specific content (optional - if not provided, Content will be used for both)
        public string? MaleContent { get; set; }
        public string? FemaleContent { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateCampaignTemplateRequest
    {
        [StringLength(200)]
        public string? Name { get; set; }

        public string? Content { get; set; }

        // Gender-specific content
        public string? MaleContent { get; set; }
        public string? FemaleContent { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }

    public class CampaignTemplateResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? MaleContent { get; set; }
        public string? FemaleContent { get; set; }
        public string? Description { get; set; }
        public bool IsSystemTemplate { get; set; }
        public bool IsActive { get; set; }
        public string? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CampaignTemplatesGroupedResponse
    {
        public List<CampaignTemplateResponse> AdminTemplates { get; set; } = new();
        public List<CampaignTemplateResponse> UserTemplates { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
