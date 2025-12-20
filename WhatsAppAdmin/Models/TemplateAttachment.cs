using System.ComponentModel.DataAnnotations;

namespace WhatsAppAdmin.Models
{
    /// <summary>
    /// Template attachment model for storing files associated with campaign templates
    /// Supports various file types including images, videos, documents, and spreadsheets
    /// </summary>
    public class TemplateAttachment
    {
        public int Id { get; set; }

        [Required]
        public int CampaignTemplateId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FileType { get; set; } = string.Empty; // MIME type

        [StringLength(50)]
        public string FileExtension { get; set; } = string.Empty;

        public long FileSize { get; set; } // Size in bytes

        [StringLength(20)]
        public string AttachmentType { get; set; } = string.Empty; // Image, Video, Document, Spreadsheet

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual CampaignTemplate CampaignTemplate { get; set; } = null!;
    }
}