namespace WhatsAppAdmin.Models.API
{
    public class UserApiKey
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KeyValue { get; set; } = string.Empty;
        public string KeyPreview { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
        public int DailyUsageCount { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class WhatsAppMessageRequest
    {
        public string Phone { get; set; } = string.Empty;
        public List<string> Messages { get; set; } = new();
        public IFormFile? AttachedFile { get; set; }
        public string? FileBase64 { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
    }

    public class SendBulkMessageRequest
    {
        public List<WhatsAppMessageRequest> Messages { get; set; } = new();
        public int DelayBetweenMessages { get; set; } = 5000;
        public bool SendImmediately { get; set; } = true;
    }

    public class ApiUsageResponse
    {
        public int TotalMessages { get; set; }
        public int TodayMessages { get; set; }
        public int RemainingQuota { get; set; }
        public DateTime LastUsed { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
    }

    public class SendMessageResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public int RemainingQuota { get; set; }
    }
}