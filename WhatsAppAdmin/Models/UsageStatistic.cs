
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppAdmin.Models
{
    public class UsageStatistic
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public DateTime Date { get; set; }

        public int MessagesSent { get; set; }

        public int MessagesDelivered { get; set; }

        public int MessagesFailed { get; set; }

        [Column(TypeName = "decimal(10, 4)")]
        public decimal TotalCost { get; set; }

        public int ApiCallsMade { get; set; }

        public virtual AdminUser User { get; set; }
    }
}
