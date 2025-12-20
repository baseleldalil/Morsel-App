using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppSender.API.Models
{
    public class ApiKey
    {
        public int Id { get; set; }

        [Required]
        [Column("Key")]
        [StringLength(64)]
        public string Key { get; set; } = string.Empty;

        [NotMapped]
        public string KeyValue { get => Key; set => Key = value ?? string.Empty; }

        [Required]
        [Column("UserId")]
        public string UserId { get; set; } = string.Empty;

        [NotMapped]
        public string UserEmail { get => User?.Email ?? string.Empty; set { /* no-op, maintained for compatibility */ } }

        [Column("SubscriptionPlanId")]
        public int SubscriptionId { get; set; }

        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }

        [Column("TotalMessagesUsed")]
        public int UsageCount { get; set; } = 0;

        [Column("DailyQuotaUsed")]
        public int DailyUsageCount { get; set; } = 0;

        [Column("LastUsageResetAt")]
        public DateTime LastResetDate { get; set; } = DateTime.UtcNow.Date;

        // Navigation properties
        public virtual Subscription? Subscription { get; set; }
        public virtual User? User { get; set; }

        public static implicit operator ApiKey(WhatsApp.Shared.Models.ApiKey v)
        {
            if (v == null) return null!;
            return new ApiKey
            {
                Id = v.Id,
                Key = v.Key,
                UserId = v.UserId,
                SubscriptionId = v.SubscriptionPlanId,
                Name = v.Name,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt,
                ExpiresAt = v.ExpiresAt,
                LastUsedAt = v.LastUsedAt,
                UsageCount = v.TotalMessagesUsed,
                DailyUsageCount = v.DailyQuotaUsed,
                LastResetDate = v.LastUsageResetAt
            };
        }
    }

    public class WhatsAppMessage
    {
        public string Phone { get; set; } = string.Empty;
        public List<string> Messages { get; set; } = new();

        public List<FileAttachment> Files { get; set; } = new();
    }

    public class FileAttachment
    {
        public string FileBase64 { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? FileType { get; set; }
    }

    public class SendMessageRequest
    {
        public List<WhatsAppMessage> Messages { get; set; } = new();

        [Obsolete("Use TimingConfig.MinDelaySeconds and MaxDelaySeconds instead")]
        public int DelayBetweenMessages { get; set; } = 5000; // milliseconds (deprecated)

        public bool SendImmediately { get; set; } = true;

        // New timing configuration with randomization support
        public TimingConfig? TimingConfig { get; set; }

        // Browser configuration settings
        public WhatsAppSender.API.Services.BrowserSettings? BrowserSettings { get; set; }
    }

    public class TimingConfig
    {
        public int MinDelaySeconds { get; set; } = 5;
        public int MaxDelaySeconds { get; set; } = 15;
        public bool UseStrongRandomization { get; set; } = true;
        public bool EnableMessageBasedPauses { get; set; } = true;
        public Dictionary<int, int>? MessagePauseRules { get; set; }
        public int VideoMinDelaySeconds { get; set; } = 20;
        public int VideoMaxDelaySeconds { get; set; } = 40;
    }

    public class SendMessageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public int RemainingQuota { get; set; }

        // Delivery tracking
        public int SentCount { get; set; }
        public int DeliveredCount { get; set; }
        public List<MessageDeliveryStatus> DeliveryStatuses { get; set; } = new();
    }

    public class MessageDeliveryStatus
    {
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Sent, Delivered, Failed
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ApiUsageStats
    {
        public int TotalMessages { get; set; }
        public int TodayMessages { get; set; }
        public int RemainingQuota { get; set; }
        public DateTime LastUsed { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
    }

    public class MessageResult
    {
        public string Phone { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class SendMessageWithTimingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<MessageResult> Results { get; set; } = new();
        public string TimingMode { get; set; } = "auto";
        public object? TimingSettings { get; set; }
    }
}