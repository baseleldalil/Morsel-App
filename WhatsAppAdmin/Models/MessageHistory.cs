
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppAdmin.Models
{
    public class MessageHistory
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public int? CampaignId { get; set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; set; }

        [Required]
        public string MessageContent { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public DateTime SentAt { get; set; }

        public string ErrorMessage { get; set; }

        public bool HasAttachment { get; set; }

        public string AttachmentName { get; set; }

        public string AttachmentType { get; set; }

        public long? AttachmentSize { get; set; }

        public string ApiKeyUsed { get; set; }

        [Column(TypeName = "decimal(10, 4)")]
        public decimal? Cost { get; set; }

        public virtual AdminUser User { get; set; }
        public virtual CampaignTemplate Campaign { get; set; }
    }
}
