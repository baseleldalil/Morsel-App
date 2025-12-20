using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Models;

namespace WhatsApp.Shared.Data
{
    public class SaaSDbContext : IdentityDbContext<ApplicationUser>
    {
        public SaaSDbContext(DbContextOptions<SaaSDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        // SaaS Core Tables
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<MessageHistory> MessageHistory { get; set; }
        public DbSet<CampaignTemplate> CampaignTemplates { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<TemplateAttachment> TemplateAttachments { get; set; }
        public DbSet<UsageStatistic> UsageStatistics { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Contact> Contacts { get; set; }

        // Timing Control Tables
        public DbSet<MessageTimingControl> MessageTimingControls { get; set; }
        public DbSet<RandomDelayRule> RandomDelayRules { get; set; }
        public DbSet<VideoTimingControl> VideoTimingControls { get; set; }

        // Advanced Timing Settings (per user)
        public DbSet<AdvancedTimingSettings> AdvancedTimingSettings { get; set; }

        // Sent Phone Numbers (for duplicate prevention)
        public DbSet<SentPhoneNumber> SentPhoneNumbers { get; set; }

        // Sent Phones (for persistent duplicate prevention - Requirement #4)
        public DbSet<SentPhone> SentPhones { get; set; }

        // Campaign Workflows (links campaigns to contacts with status tracking)
        public DbSet<CampaignWorkflow> CampaignWorkflows { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure decimal precision for all money fields
            builder.Entity<SubscriptionPlan>()
                .Property(e => e.Price)
                .HasPrecision(18, 2);

            builder.Entity<UserSubscription>()
                .Property(e => e.AmountPaid)
                .HasPrecision(18, 2);

            builder.Entity<MessageHistory>()
                .Property(e => e.Cost)
                .HasPrecision(10, 4);

            builder.Entity<UsageStatistic>()
                .Property(e => e.TotalCost)
                .HasPrecision(10, 4);

            builder.Entity<PaymentTransaction>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            // Configure relationships
            builder.Entity<UserSubscription>()
                .HasOne(us => us.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserSubscription>()
                .HasOne(us => us.SubscriptionPlan)
                .WithMany(sp => sp.UserSubscriptions)
                .HasForeignKey(us => us.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApiKey>()
                .HasOne(ak => ak.User)
                .WithMany(u => u.ApiKeys)
                .HasForeignKey(ak => ak.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ApiKey>()
                .HasOne(ak => ak.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(ak => ak.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MessageHistory>()
                .HasOne(mh => mh.User)
                .WithMany(u => u.MessageHistory)
                .HasForeignKey(mh => mh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MessageHistory>()
                .HasOne(mh => mh.Campaign)
                .WithMany(ct => ct.Messages)
                .HasForeignKey(mh => mh.CampaignId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<CampaignTemplate>()
                .HasOne(ct => ct.User)
                .WithMany(u => u.CampaignTemplates)
                .HasForeignKey(ct => ct.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Campaign>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Campaign>()
                .HasOne(c => c.CampaignTemplate)
                .WithMany()
                .HasForeignKey(c => c.CampaignTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Campaign>()
                .HasIndex(c => new { c.UserId, c.Status });

            builder.Entity<Campaign>()
                .HasIndex(c => c.CreatedAt);

            builder.Entity<Contact>()
                .HasOne(c => c.Campaign)
                .WithMany()
                .HasForeignKey(c => c.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<TemplateAttachment>()
                .HasOne(ta => ta.CampaignTemplate)
                .WithMany(ct => ct.Attachments)
                .HasForeignKey(ta => ta.CampaignTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UsageStatistic>()
                .HasOne(us => us.User)
                .WithMany()
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PaymentTransaction>()
                .HasOne(pt => pt.User)
                .WithMany()
                .HasForeignKey(pt => pt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PaymentTransaction>()
                .HasOne(pt => pt.Subscription)
                .WithMany()
                .HasForeignKey(pt => pt.SubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);

            // Indexes for performance
            builder.Entity<ApiKey>()
                .HasIndex(ak => ak.Key)
                .IsUnique();

            builder.Entity<ApiKey>()
                .HasIndex(ak => new { ak.UserId, ak.IsActive });

            // Ensure EF ignores compatibility convenience properties that are not mapped to DB columns
            builder.Entity<ApiKey>().Ignore(ak => ak.KeyValue);
            builder.Entity<ApiKey>().Ignore(ak => ak.UserEmail);
            builder.Entity<ApiKey>().Ignore(ak => ak.SubscriptionId);
            builder.Entity<ApiKey>().Ignore(ak => ak.Subscription);
            builder.Entity<ApiKey>().Ignore(ak => ak.UsageCount);
            builder.Entity<ApiKey>().Ignore(ak => ak.DailyUsageCount);
            builder.Entity<ApiKey>().Ignore(ak => ak.LastResetAt);

            builder.Entity<MessageHistory>()
                .HasIndex(mh => new { mh.UserId, mh.SentAt });

            builder.Entity<UsageStatistic>()
                .HasIndex(us => new { us.UserId, us.Date })
                .IsUnique();

            builder.Entity<SystemSetting>()
                .HasIndex(ss => ss.Key)
                .IsUnique();

            // Configure Contact
            builder.Entity<Contact>()
                .Property(c => c.UserId)
                .HasMaxLength(450);

            builder.Entity<Contact>()
                .Property(c => c.ApiKeyId)
                .HasMaxLength(450);

            builder.Entity<Contact>()
                .HasIndex(c => c.FormattedPhone);

            builder.Entity<Contact>()
                .HasIndex(c => c.UserId);

            builder.Entity<Contact>()
                .HasIndex(c => c.ApiKeyId);

            builder.Entity<Contact>()
                .HasIndex(c => c.Status);

            // Configure SentPhone for duplicate prevention (Requirement #4)
            builder.Entity<SentPhone>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.PhoneNumber })
                      .IsUnique()
                      .HasDatabaseName("IDX_SentPhones_UserPhone");

                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IDX_SentPhones_UserId");

                entity.HasIndex(e => e.CampaignId)
                      .HasDatabaseName("IDX_SentPhones_CampaignId");

                entity.HasIndex(e => e.SentAt)
                      .HasDatabaseName("IDX_SentPhones_SentAt");
            });

            // Configure timing control models
            builder.Entity<MessageTimingControl>(b =>
            {
                b.HasIndex(e => e.SubscriptionPlanId);
                b.HasIndex(e => e.IsActive);
                b.HasOne(e => e.SubscriptionPlan)
                    .WithMany()
                    .HasForeignKey(e => e.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<RandomDelayRule>(b =>
            {
                b.HasIndex(e => e.SubscriptionPlanId);
                b.HasIndex(e => e.IsActive);
                b.HasIndex(e => e.Priority);
                b.HasOne(e => e.SubscriptionPlan)
                    .WithMany()
                    .HasForeignKey(e => e.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<VideoTimingControl>(b =>
            {
                b.HasIndex(e => e.SubscriptionPlanId);
                b.HasIndex(e => e.IsActive);
                b.HasOne(e => e.SubscriptionPlan)
                    .WithMany()
                    .HasForeignKey(e => e.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure advanced timing settings
            builder.Entity<AdvancedTimingSettings>(b =>
            {
                b.Property(e => e.MinDelaySeconds).HasPrecision(18, 2);
                b.Property(e => e.MaxDelaySeconds).HasPrecision(18, 2);
                b.Property(e => e.MinBreakMinutes).HasPrecision(18, 2);
                b.Property(e => e.MaxBreakMinutes).HasPrecision(18, 2);
                b.HasIndex(e => e.UserId).IsUnique(); // One settings per user
            });

            // Configure SentPhoneNumbers for duplicate prevention
            builder.Entity<SentPhoneNumber>(b =>
            {
                b.Property(e => e.UserId).HasMaxLength(450);
                b.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(e => new { e.UserId, e.PhoneNumber }).IsUnique(); // One record per user-phone combination
                b.HasIndex(e => e.PhoneNumber); // For quick lookups
            });

            // Configure Campaign.Contacts relationship
            builder.Entity<Campaign>()
                .HasMany(c => c.Contacts)
                .WithOne(c => c.Campaign)
                .HasForeignKey(c => c.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure CampaignWorkflow entity
            builder.Entity<CampaignWorkflow>(entity =>
            {
                // Indexes for performance
                entity.HasIndex(e => e.CampaignId)
                      .HasDatabaseName("IDX_CampaignWorkflows_CampaignId");

                entity.HasIndex(e => e.ContactId)
                      .HasDatabaseName("IDX_CampaignWorkflows_ContactId");

                entity.HasIndex(e => e.WorkflowStatus)
                      .HasDatabaseName("IDX_CampaignWorkflows_Status");

                entity.HasIndex(e => new { e.CampaignId, e.ContactId })
                      .IsUnique()
                      .HasDatabaseName("IDX_CampaignWorkflows_CampaignContact");

                entity.HasIndex(e => e.AddedAt)
                      .HasDatabaseName("IDX_CampaignWorkflows_AddedAt");

                // Relationship: Campaign 1:N CampaignWorkflow
                entity.HasOne(e => e.Campaign)
                      .WithMany(c => c.CampaignWorkflows)
                      .HasForeignKey(e => e.CampaignId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship: Contact 1:N CampaignWorkflow
                entity.HasOne(e => e.Contact)
                      .WithMany()
                      .HasForeignKey(e => e.ContactId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed default data
            SeedData(builder);
        }

        private void SeedData(ModelBuilder builder)
        {
            var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Default Subscription Plans with Enhanced Features
            builder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Free Trial",
                    Description = "Try our service free for 14 days - no credit card required",
                    Price = 0m,
                    MaxMessagesPerDay = 50,
                    MaxApiKeys = 1,
                    MaxCampaignTemplates = 3,
                    MaxContactsPerCampaign = 25,
                    TrialDurationDays = 14,
                    BillingCycleDays = 30,
                    GracePeriodDays = 3,
                    HasPrioritySupport = false,
                    HasCustomIntegrations = false,
                    HasAdvancedAnalytics = false,
                    HasScheduledMessages = false,
                    HasTemplateVariables = false,
                    HasMultiLanguageSupport = false,
                    HasWhatsAppBotIntegration = false,
                    HasCustomBranding = false,
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = now
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "Starter",
                    Description = "Perfect for small businesses getting started with WhatsApp marketing",
                    Price = 9.99m,
                    MaxMessagesPerDay = 100,
                    MaxApiKeys = 2,
                    MaxCampaignTemplates = 10,
                    MaxContactsPerCampaign = 100,
                    TrialDurationDays = 7,
                    BillingCycleDays = 30,
                    GracePeriodDays = 3,
                    HasPrioritySupport = false,
                    HasCustomIntegrations = false,
                    HasAdvancedAnalytics = false,
                    HasScheduledMessages = true,
                    HasTemplateVariables = true,
                    HasMultiLanguageSupport = false,
                    HasWhatsAppBotIntegration = false,
                    HasCustomBranding = false,
                    IsActive = true,
                    DisplayOrder = 2,
                    CreatedAt = now
                },
                new SubscriptionPlan
                {
                    Id = 3,
                    Name = "Professional",
                    Description = "Ideal for growing businesses with moderate messaging needs",
                    Price = 29.99m,
                    MaxMessagesPerDay = 500,
                    MaxApiKeys = 5,
                    MaxCampaignTemplates = 25,
                    MaxContactsPerCampaign = 500,
                    TrialDurationDays = 7,
                    BillingCycleDays = 30,
                    GracePeriodDays = 5,
                    HasPrioritySupport = true,
                    HasCustomIntegrations = false,
                    HasAdvancedAnalytics = true,
                    HasScheduledMessages = true,
                    HasTemplateVariables = true,
                    HasMultiLanguageSupport = true,
                    HasWhatsAppBotIntegration = false,
                    HasCustomBranding = false,
                    IsActive = true,
                    DisplayOrder = 3,
                    CreatedAt = now
                },
                new SubscriptionPlan
                {
                    Id = 4,
                    Name = "Business",
                    Description = "For large businesses with high-volume messaging requirements",
                    Price = 99.99m,
                    MaxMessagesPerDay = 2000,
                    MaxApiKeys = 10,
                    MaxCampaignTemplates = 100,
                    MaxContactsPerCampaign = 2000,
                    TrialDurationDays = 14,
                    BillingCycleDays = 30,
                    GracePeriodDays = 7,
                    HasPrioritySupport = true,
                    HasCustomIntegrations = true,
                    HasAdvancedAnalytics = true,
                    HasScheduledMessages = true,
                    HasTemplateVariables = true,
                    HasMultiLanguageSupport = true,
                    HasWhatsAppBotIntegration = true,
                    HasCustomBranding = true,
                    IsActive = true,
                    DisplayOrder = 4,
                    CreatedAt = now
                },
                new SubscriptionPlan
                {
                    Id = 5,
                    Name = "Enterprise",
                    Description = "Unlimited messaging for enterprise-level operations with dedicated support",
                    Price = 299.99m,
                    MaxMessagesPerDay = 10000,
                    MaxApiKeys = 25,
                    MaxCampaignTemplates = -1, // Unlimited
                    MaxContactsPerCampaign = 10000,
                    TrialDurationDays = 14,
                    BillingCycleDays = 30,
                    GracePeriodDays = 14,
                    HasPrioritySupport = true,
                    HasCustomIntegrations = true,
                    HasAdvancedAnalytics = true,
                    HasScheduledMessages = true,
                    HasTemplateVariables = true,
                    HasMultiLanguageSupport = true,
                    HasWhatsAppBotIntegration = true,
                    HasCustomBranding = true,
                    IsActive = true,
                    DisplayOrder = 5,
                    CreatedAt = now
                }
            );

            builder.Entity<SystemSetting>().HasData(
                new SystemSetting
                {
                    Id = 1,
                    Key = "SAAS_NAME",
                    Value = "WhatsApp SaaS Platform",
                    Description = "The name of the SaaS platform",
                    Category = "General",
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = 2,
                    Key = "WHATSAPP_API_URL",
                    Value = "https://localhost:7001/api",
                    Description = "Base URL for WhatsApp API service",
                    Category = "WhatsApp",
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = 3,
                    Key = "MESSAGE_COST_PER_UNIT",
                    Value = "0.05",
                    Description = "Cost per message in USD",
                    Category = "Billing",
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = 4,
                    Key = "DEFAULT_TIMEZONE",
                    Value = "UTC",
                    Description = "Default timezone for the application",
                    Category = "General",
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    Id = 5,
                    Key = "MAX_FILE_UPLOAD_SIZE",
                    Value = "10485760",
                    Description = "Maximum file upload size in bytes (10MB)",
                    Category = "WhatsApp",
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }
    }
}