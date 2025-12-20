
using System;
using System.ComponentModel.DataAnnotations;

namespace WhatsAppAdmin.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string Category { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string UpdatedBy { get; set; }
    }
}
