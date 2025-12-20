
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppAdmin.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public int? SubscriptionId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; }

        public string PaymentReference { get; set; }

        public string PaymentGatewayResponse { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public virtual AdminUser User { get; set; }
        public virtual UserSubscription Subscription { get; set; }
    }
}
