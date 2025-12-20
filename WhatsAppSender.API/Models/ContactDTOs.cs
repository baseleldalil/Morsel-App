using System.ComponentModel.DataAnnotations;
using WhatsApp.Shared.Models;

namespace WhatsAppSender.API.Models
{
    // DTOs for Contact Status Updates
    public class UpdateContactStatusRequest
    {
        [Required]
        public ContactStatus Status { get; set; }

        [StringLength(500)]
        public string? IssueDescription { get; set; }
    }

    public class BulkUpdateContactStatusRequest
    {
        [Required]
        public List<int> ContactIds { get; set; } = new List<int>();

        [Required]
        public ContactStatus Status { get; set; }

        [StringLength(500)]
        public string? IssueDescription { get; set; }
    }

    // DTOs for Contact Status Filtering
    public class ContactStatusFilterRequest
    {
        /// <summary>
        /// Filter type: "All", "Pending", "Sent", "HasIssues", "NotInterested", "Responded"
        /// </summary>
        public string FilterType { get; set; } = "All";

        /// <summary>
        /// Optional: specific status values to filter by (if FilterType is "Custom")
        /// </summary>
        public List<ContactStatus>? Statuses { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? Search { get; set; }
    }

    // DTOs for Contact Statistics
    public class ContactStatusStatistics
    {
        public int TotalContacts { get; set; }
        public int PendingCount { get; set; }
        public int SendingCount { get; set; }
        public int SentCount { get; set; }
        public int DeliveredCount { get; set; }
        public int FailedCount { get; set; }
        public int NotValidCount { get; set; }
        public int HasIssuesCount { get; set; }
        public int BlockedCount { get; set; }
        public int NotInterestedCount { get; set; }
        public int RespondedCount { get; set; }
    }
}
