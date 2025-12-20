using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsApp.Shared.Models
{
    // Base User class for the entire SaaS system
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? CompanyName { get; set; }
        public override string? PhoneNumber { get; set; }

        // Navigation properties
        public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
        public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
        public ICollection<MessageHistory> MessageHistory { get; set; } = new List<MessageHistory>();
        public ICollection<CampaignTemplate> CampaignTemplates { get; set; } = new List<CampaignTemplate>();
    }

    // Subscription Plans - Global for entire SaaS
    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int MaxMessagesPerDay { get; set; }
        public int MaxApiKeys { get; set; } = 5;
        public int MaxCampaignTemplates { get; set; } = 10;
        public int MaxContactsPerCampaign { get; set; } = 100;

        // Enhanced Features
        public bool HasPrioritySupport { get; set; } = false;
        public bool HasCustomIntegrations { get; set; } = false;
        public bool HasAdvancedAnalytics { get; set; } = false;
        public bool HasScheduledMessages { get; set; } = false;
        public bool HasTemplateVariables { get; set; } = false;
        public bool HasMultiLanguageSupport { get; set; } = false;
        public bool HasWhatsAppBotIntegration { get; set; } = false;
        public bool HasCustomBranding { get; set; } = false;

        // Trial and Billing
        public int TrialDurationDays { get; set; } = 0;
        public int BillingCycleDays { get; set; } = 30; // 30 days, 90 days, 365 days
        public int GracePeriodDays { get; set; } = 3; // Days after expiration before service stops

        // Discount support
        public decimal? DiscountPercentage { get; set; }
        public DateTime? DiscountValidUntil { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0; // For sorting in UI

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }

    // User Subscriptions
    public class UserSubscription
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int SubscriptionPlanId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        public string PaymentMethod { get; set; } = "Demo"; // Demo, Stripe, PayPal, etc.
        public string? PaymentReference { get; set; }

        // Enhanced Subscription Status
        public bool IsActive { get; set; } = true;
        public bool AutoRenew { get; set; } = true;
        public bool IsTrialPeriod { get; set; } = false;
        public bool IsInGracePeriod { get; set; } = false;

        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Expired, Cancelled, Suspended, InTrial, InGrace

        // Renewal tracking
        public DateTime? NextBillingDate { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public DateTime? GracePeriodEndsAt { get; set; }

        // Usage tracking
        public int CurrentPeriodMessages { get; set; } = 0;
        public DateTime? LastUsageResetAt { get; set; }

        // Cancellation details
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public string? CancelledBy { get; set; } // UserId who cancelled

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    }

    // API Keys for WhatsApp API access
    public class ApiKey
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int SubscriptionPlanId { get; set; }
        public int DailyQuotaUsed { get; set; } = 0;
        public int TotalMessagesUsed { get; set; } = 0;
        public DateTime? LastUsedAt { get; set; }
        public DateTime LastUsageResetAt { get; set; } = DateTime.UtcNow.Date;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        // Compatibility / convenience properties used across the solution
        // These map existing canonical properties to the names expected by other projects
        private string? _userEmailBackup;

        [NotMapped]
        public string KeyValue
        {
            get => Key;
            set => Key = value ?? string.Empty;
        }

        [NotMapped]
        public string UserEmail
        {
            get => User?.Email ?? _userEmailBackup ?? string.Empty;
            set => _userEmailBackup = value;
        }

        [NotMapped]
        public int SubscriptionId
        {
            get => SubscriptionPlanId;
            set => SubscriptionPlanId = value;
        }

        [NotMapped]
        public SubscriptionPlan? Subscription
        {
            get => SubscriptionPlan;
            set { if (value != null) SubscriptionPlan = value; }
        }

        [NotMapped]
        public int UsageCount
        {
            get => TotalMessagesUsed;
            set => TotalMessagesUsed = value;
        }

        [NotMapped]
        public int DailyUsageCount
        {
            get => DailyQuotaUsed;
            set => DailyQuotaUsed = value;
        }

        [NotMapped]
        public DateTime LastResetAt
        {
            get => LastUsageResetAt;
            set => LastUsageResetAt = value;
        }
    }

    // Message History
    public class MessageHistory
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string MessageContent { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Sent, Failed, Delivered

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public string? ErrorMessage { get; set; }

        public bool HasAttachment { get; set; } = false;
        public string? AttachmentName { get; set; }
        public string? AttachmentType { get; set; }
        public long? AttachmentSize { get; set; }

        public string? ApiKeyUsed { get; set; }
        public int? CampaignId { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? Cost { get; set; } // Cost per message for billing

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public CampaignTemplate? Campaign { get; set; }
    }

    // Campaign Templates (from Admin panel)
    public class CampaignTemplate
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; // Admin or user who created it

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string MessageTemplate { get; set; } = string.Empty;

        public string? MaleContent { get; set; }
        public string? FemaleContent { get; set; }
        public bool AutoGenerateGenderVersions { get; set; } = true;

        // Template categorization
        [StringLength(50)]
        public string Category { get; set; } = "General"; // General, Marketing, Support, Transactional

        public bool IsActive { get; set; } = true;
        public bool IsGlobal { get; set; } = false; // Available to all users
        public bool IsSystemTemplate { get; set; } = false; // Cannot be deleted

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Attachment support
        public bool HasAttachment { get; set; } = false;
        public string? AttachmentPath { get; set; }
        public string? AttachmentName { get; set; }
        public string? AttachmentType { get; set; }

        // Template Variables Support (e.g., {{name}}, {{company}}, {{date}})
        public bool SupportsVariables { get; set; } = false;
        public string? AvailableVariables { get; set; } // JSON array of variable names

        // Scheduling Support
        public bool IsScheduled { get; set; } = false;
        public DateTime? ScheduledAt { get; set; }
        public string? ScheduleRecurrence { get; set; } // Daily, Weekly, Monthly, Once
        public DateTime? LastScheduledRun { get; set; }
        public DateTime? NextScheduledRun { get; set; }

        // Translation Support
        public bool SupportsMultiLanguage { get; set; } = false;
        [StringLength(10)]
        public string? DefaultLanguage { get; set; } = "en";
        public string? TranslatedTemplates { get; set; } // JSON object with language codes as keys

        // Usage statistics
        public int TimesUsed { get; set; } = 0;
        public DateTime? LastUsedAt { get; set; }
        public decimal SuccessRate { get; set; } = 0; // Percentage of successful deliveries

        // Targeting
        public string? TargetAudience { get; set; } // JSON object for audience filters
        public int? MaxRecipientsPerBatch { get; set; } = 50;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public ICollection<MessageHistory> Messages { get; set; } = new List<MessageHistory>();
        public ICollection<TemplateAttachment> Attachments { get; set; } = new List<TemplateAttachment>();
    }

    // Template Attachments
    public class TemplateAttachment
    {
        public int Id { get; set; }

        public int CampaignTemplateId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public CampaignTemplate CampaignTemplate { get; set; } = null!;
    }

    // Usage Statistics (for analytics)
    public class UsageStatistic
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime Date { get; set; }
        public int MessagesSent { get; set; } = 0;
        public int MessagesDelivered { get; set; } = 0;
        public int MessagesFailed { get; set; } = 0;

        [Column(TypeName = "decimal(10,4)")]
        public decimal TotalCost { get; set; } = 0;

        public int ApiCallsMade { get; set; } = 0;

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }

    // System Settings (for SaaS configuration)
    public class SystemSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string Category { get; set; } = "General"; // General, WhatsApp, Billing, etc.

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
    }

    // Payment Transactions
    public class PaymentTransaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int? SubscriptionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Demo";

        public string? PaymentReference { get; set; }
        public string? PaymentGatewayResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public UserSubscription? Subscription { get; set; }
    }

    // Contact Status Enum
    public enum ContactStatus
    {
        Pending = 0,           // Imported, waiting to be sent
        Sending = 1,           // Currently being sent
        Sent = 2,              // Successfully sent
        Delivered = 3,         // Confirmed delivered to recipient
        Failed = 4,            // Failed to send
        NotValid = 5,          // Invalid phone number
        HasIssues = 6,         // Other issues
        Blocked = 7,           // Number blocked/opted out
        NotInterested = 8,     // Contact not interested
        Responded = 9          // Contact responded
    }

    // Campaign Status Enum
    public enum CampaignStatus
    {
        Pending = 0,           // Created but not started
        Running = 1,           // Currently executing
        Paused = 2,            // Paused by user
        Stopped = 3,           // Stopped by user (can't resume)
        Completed = 4,         // Finished successfully
        Failed = 5,            // Failed with errors
        New = 6,               // Newly created campaign
        Draft = 7,             // Draft campaign (not yet ready)
        Scheduled = 8,         // Scheduled for future execution
        Cancelled = 9          // Cancelled by user
    }

    // Workflow Status Enum - For CampaignWorkflow tracking
    public enum WorkflowStatus
    {
        Pending = 0,           // Added to workflow, waiting to be processed
        Processing = 1,        // Currently being processed
        Sent = 2,              // Message sent successfully
        Delivered = 3,         // Message delivered to recipient
        Failed = 4,            // Failed to send
        Bounced = 5,           // Message bounced back
        Opened = 6,            // Message was opened/read
        Clicked = 7 ,           // Link in message was clickedm
        New =8 
    }

    // Campaign - Tracks message sending campaigns
    public class Campaign
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int? CampaignTemplateId { get; set; }

        public CampaignStatus Status { get; set; } = CampaignStatus.Pending;

        // Campaign metrics
        public int TotalContacts { get; set; } = 0;
        public int MessagesSent { get; set; } = 0;
        public int MessagesDelivered { get; set; } = 0;
        public int MessagesFailed { get; set; } = 0;
        public int CurrentProgress { get; set; } = 0; // Index of last processed contact

        // Enhanced pause/resume/stop tracking
        public int? CurrentContactId { get; set; } // Exact contact being processed (for Pause/Resume)
        public int? LastCompletedContactId { get; set; } // Last successfully sent contact (for Stop/Restart)
        public int? PausedAtContactId { get; set; } // Contact ID where campaign was paused

        // Duplicate prevention
        [StringLength(20)]
        public string DuplicatePreventionMode { get; set; } = "Persistent"; // Persistent or InMemory

        // Browser selection
        [StringLength(20)]
        public string? SelectedBrowser { get; set; } // Chrome, Firefox, Edge

        // Timing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Error tracking
        public string? LastError { get; set; }
        public int ErrorCount { get; set; } = 0;

        // Campaign settings
        public string? MessageContent { get; set; }
        public bool UseGenderTemplates { get; set; } = false;

        // Gender-specific message content
        public string? MaleContent { get; set; }
        public string? FemaleContent { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public CampaignTemplate? CampaignTemplate { get; set; }
        public ICollection<MessageHistory> Messages { get; set; } = new List<MessageHistory>();
        public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public ICollection<CampaignWorkflow> CampaignWorkflows { get; set; } = new List<CampaignWorkflow>();
    }

    // Sent Phone Numbers - For persistent duplicate prevention
    public class SentPhoneNumber
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime FirstSentAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSentAt { get; set; } = DateTime.UtcNow;
        public int SendCount { get; set; } = 1;

        public int? LastCampaignId { get; set; }

        [StringLength(50)]
        public string? LastStatus { get; set; } // Sent, Delivered, Failed, etc.

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }

    /// <summary>
    /// SentPhone model for duplicate prevention tracking (Requirement #4)
    /// Tracks all phone numbers sent to by a user to prevent duplicate sends
    /// </summary>
    public class SentPhone
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public int? CampaignId { get; set; }

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public string? MessageContent { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "sent"; // sent, failed, responded, not-interested

        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual Campaign? Campaign { get; set; }
    }

    public class Contact
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string ArabicName { get; set; } = string.Empty;

        [StringLength(100)]
        public string EnglishName { get; set; } = string.Empty;

        // Enhanced phone handling for multi-number support
        [StringLength(200)]
        public string? PhoneRaw { get; set; } // Original phone from Excel (may contain multiple numbers)

        [Required]
        [StringLength(100)]
        public string FormattedPhone { get; set; } = string.Empty; // Cleaned, normalized phone

        [StringLength(100)]
        public string? PhoneNormalized { get; set; } // First valid extracted phone (>=10 digits)

        [Required]
        [StringLength(1)]
        public string Gender { get; set; } = "U"; // M, F, or U (Unknown) - MUST be M or F to send

        public ContactStatus Status { get; set; } = ContactStatus.Pending;

        [StringLength(500)]
        public string? IssueDescription { get; set; }

        public DateTime? LastMessageSentAt { get; set; }
        public DateTime? LastStatusUpdateAt { get; set; }
        public int SendAttemptCount { get; set; } = 0;

        // Row selection and ordering
        public bool IsSelected { get; set; } = true; // Default selected for campaign
        public int OriginalRowIndex { get; set; } = 0; // Preserve Excel row order (1-based)

        // Custom data columns (for variable substitution)
        public string? City { get; set; }
        public string? Company { get; set; }
        public string? CustomField1 { get; set; }
        public string? CustomField2 { get; set; }
        public string? CustomField3 { get; set; }

        // Attachment support (images/files to send with message)
        [StringLength(500)]
        public string? AttachmentPath { get; set; }         // Full file path on server

        [StringLength(255)]
        public string? AttachmentFileName { get; set; }     // Original file name

        [StringLength(100)]
        public string? AttachmentType { get; set; }         // Image, Document, Video

        public long? AttachmentSize { get; set; }           // File size in bytes

        public DateTime? AttachmentUploadedAt { get; set; } // When file was uploaded

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key - links to ApiKey
        [Required]
        public string ApiKeyId { get; set; } = string.Empty;

        // User ID for direct association
        [Required]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string Number { get; set; } = string.Empty;

        // Campaign association
        public int? CampaignId { get; set; }
        public Campaign? Campaign { get; set; }
    }

    /// <summary>
    /// CampaignWorkflow - Links campaigns to contacts and tracks workflow status
    /// Represents the processing state of each contact within a campaign
    /// </summary>
    public class CampaignWorkflow
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to Campaign
        /// </summary>
        [Required]
        public int CampaignId { get; set; }

        /// <summary>
        /// Foreign key to Contact
        /// </summary>
        [Required]
        public int ContactId { get; set; }

        /// <summary>
        /// Current workflow status for this contact in the campaign
        /// </summary>
        public WorkflowStatus WorkflowStatus { get; set; } = WorkflowStatus.Pending;

        /// <summary>
        /// When this contact was added to the campaign workflow
        /// </summary>
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this contact was last processed
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Error message if workflow failed
        /// </summary>
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Number of retry attempts
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// When delivery was confirmed (for Delivered status)
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// When message was opened (for Opened status)
        /// </summary>
        public DateTime? OpenedAt { get; set; }

        /// <summary>
        /// When link was clicked (for Clicked status)
        /// </summary>
        public DateTime? ClickedAt { get; set; }

        // Message Content Fields
        /// <summary>
        /// Message content for male contacts
        /// </summary>
        public string? MaleMessage { get; set; }

        /// <summary>
        /// Message content for female contacts
        /// </summary>
        public string? FemaleMessage { get; set; }

        // Attachment Fields (Base64 encoded)
        /// <summary>
        /// Base64 encoded attachment data (supports PNG, JPG, images, videos)
        /// </summary>
        public string? AttachmentBase64 { get; set; }

        /// <summary>
        /// Original attachment file name
        /// </summary>
        [StringLength(255)]
        public string? AttachmentFileName { get; set; }

        /// <summary>
        /// MIME type of the attachment (e.g., image/png, image/jpeg, video/mp4)
        /// </summary>
        [StringLength(100)]
        public string? AttachmentContentType { get; set; }

        /// <summary>
        /// Size of the attachment in bytes (before base64 encoding)
        /// </summary>
        public long? AttachmentSize { get; set; }

        /// <summary>
        /// Type of attachment: Image, Video, Document
        /// </summary>
        [StringLength(50)]
        public string? AttachmentType { get; set; }

        // Navigation properties
        public Campaign Campaign { get; set; } = null!;
        public Contact Contact { get; set; } = null!;
    }
}