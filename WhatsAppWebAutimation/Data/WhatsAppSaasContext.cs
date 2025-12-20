using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebAutomation.Data.Entities;

namespace WhatsAppWebAutomation.Data;

public partial class WhatsAppSaasContext : DbContext
{
    public WhatsAppSaasContext()
    {
    }

    public WhatsAppSaasContext(DbContextOptions<WhatsAppSaasContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdvancedTimingSetting> AdvancedTimingSettings { get; set; }

    public virtual DbSet<ApiKey> ApiKeys { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<Campaign> Campaigns { get; set; }

    public virtual DbSet<CampaignTemplate> CampaignTemplates { get; set; }

    public virtual DbSet<CampaignWorkflow> CampaignWorkflows { get; set; }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<MessageHistory> MessageHistories { get; set; }

    public virtual DbSet<MessageTimingControl> MessageTimingControls { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<RandomDelayRule> RandomDelayRules { get; set; }

    public virtual DbSet<SentPhone> SentPhones { get; set; }

    public virtual DbSet<SentPhoneNumber> SentPhoneNumbers { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<TemplateAttachment> TemplateAttachments { get; set; }

    public virtual DbSet<UsageStatistic> UsageStatistics { get; set; }

    public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }

    public virtual DbSet<VideoTimingControl> VideoTimingControls { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Connection string is configured via DI in Program.cs
        // This method is only called when not using DI (e.g., EF Core tools)
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdvancedTimingSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AdvancedTimingSettings_pkey");

            entity.ToTable(tb => tb.HasComment("Advanced timing settings with true randomization and decimal delays"));

            entity.HasIndex(e => e.UserId, "IX_AdvancedTimingSettings_UserId").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(now() AT TIME ZONE 'UTC'::text)")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DecimalPrecision)
                .HasDefaultValue(1)
                .HasComment("Number of decimal places for randomization (1-3): 1=32.7s, 2=32.73s, 3=32.735s");
            entity.Property(e => e.EnableRandomBreaks)
                .HasDefaultValue(true)
                .HasComment("Enable random breaks after sending multiple messages");
            entity.Property(e => e.MaxBreakMinutes)
                .HasPrecision(18, 2)
                .HasDefaultValueSql("9.0")
                .HasComment("Maximum break duration in minutes (supports decimals, e.g., 9.7)");
            entity.Property(e => e.MaxDelaySeconds)
                .HasPrecision(18, 2)
                .HasDefaultValueSql("60.0")
                .HasComment("Maximum delay between messages in seconds (supports decimals, e.g., 60.8)");
            entity.Property(e => e.MaxMessagesBeforeBreak)
                .HasDefaultValue(20)
                .HasComment("Maximum number of messages before triggering a break (e.g., 20)");
            entity.Property(e => e.MinBreakMinutes)
                .HasPrecision(18, 2)
                .HasDefaultValueSql("4.0")
                .HasComment("Minimum break duration in minutes (supports decimals, e.g., 4.2)");
            entity.Property(e => e.MinDelaySeconds)
                .HasPrecision(18, 2)
                .HasDefaultValueSql("30.0")
                .HasComment("Minimum delay between messages in seconds (supports decimals, e.g., 30.5)");
            entity.Property(e => e.MinMessagesBeforeBreak)
                .HasDefaultValue(13)
                .HasComment("Minimum number of messages before triggering a break (e.g., 13)");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(now() AT TIME ZONE 'UTC'::text)")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.UseDecimalRandomization)
                .HasDefaultValue(true)
                .HasComment("Use strong randomization with decimal values (true: 32.7s, false: 32s)");
            entity.Property(e => e.UserId).HasMaxLength(450);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(e => e.Key, "IX_ApiKeys_Key").IsUnique();

            entity.HasIndex(e => e.SubscriptionPlanId, "IX_ApiKeys_SubscriptionPlanId");

            entity.HasIndex(e => new { e.UserId, e.IsActive }, "IX_ApiKeys_UserId_IsActive");

            entity.Property(e => e.Key).HasMaxLength(64);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.ApiKeys)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.User).WithMany(p => p.ApiKeys).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasIndex(e => e.CampaignTemplateId, "IX_Campaigns_CampaignTemplateId");

            entity.HasIndex(e => e.CreatedAt, "IX_Campaigns_CreatedAt");

            entity.HasIndex(e => new { e.UserId, e.Status }, "IX_Campaigns_UserId_Status");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DuplicatePreventionMode)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Persistent'::character varying");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.SelectedBrowser).HasMaxLength(20);

            entity.HasOne(d => d.CampaignTemplate).WithMany(p => p.Campaigns)
                .HasForeignKey(d => d.CampaignTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.User).WithMany(p => p.Campaigns).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<CampaignTemplate>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_CampaignTemplates_UserId");

            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.DefaultLanguage).HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.User).WithMany(p => p.CampaignTemplates).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<CampaignWorkflow>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CampaignWorkflows_pkey");

            entity.HasIndex(e => e.AddedAt, "IDX_CampaignWorkflows_AddedAt");

            entity.HasIndex(e => new { e.CampaignId, e.ContactId }, "IDX_CampaignWorkflows_CampaignContact").IsUnique();

            entity.HasIndex(e => e.CampaignId, "IDX_CampaignWorkflows_CampaignId");

            entity.HasIndex(e => e.ContactId, "IDX_CampaignWorkflows_ContactId");

            entity.HasIndex(e => e.WorkflowStatus, "IDX_CampaignWorkflows_Status");

            entity.Property(e => e.AddedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.AttachmentContentType).HasMaxLength(100);
            entity.Property(e => e.AttachmentFileName).HasMaxLength(255);
            entity.Property(e => e.AttachmentType).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.WorkflowStatus).HasDefaultValue(0);

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignWorkflows).HasForeignKey(d => d.CampaignId);

            entity.HasOne(d => d.Contact).WithMany(p => p.CampaignWorkflows).HasForeignKey(d => d.ContactId);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasIndex(e => e.ApiKeyId, "IDX_Contacts_ApiKeyId");

            entity.HasIndex(e => e.CampaignId, "IDX_Contacts_CampaignId");

            entity.HasIndex(e => e.Status, "IDX_Contacts_Status");

            entity.HasIndex(e => e.UserId, "IDX_Contacts_UserId");

            entity.HasIndex(e => e.ApiKeyId, "IX_Contacts_ApiKeyId");

            entity.HasIndex(e => e.CampaignId, "IX_Contacts_CampaignId");

            entity.HasIndex(e => e.FormattedPhone, "IX_Contacts_FormattedPhone");

            entity.HasIndex(e => e.Status, "IX_Contacts_Status");

            entity.HasIndex(e => e.UserId, "IX_Contacts_UserId");

            entity.Property(e => e.ApiKeyId).HasMaxLength(450);
            entity.Property(e => e.ArabicName).HasMaxLength(100);
            entity.Property(e => e.AttachmentFileName).HasMaxLength(255);
            entity.Property(e => e.AttachmentPath).HasMaxLength(500);
            entity.Property(e => e.AttachmentType).HasMaxLength(100);
            entity.Property(e => e.AttachmentUploadedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.EnglishName).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.FormattedPhone).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(1);
            entity.Property(e => e.IsSelected).HasDefaultValue(true);
            entity.Property(e => e.IssueDescription).HasMaxLength(500);
            entity.Property(e => e.LastAttempt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Number).HasMaxLength(20);
            entity.Property(e => e.OriginalRowIndex).HasDefaultValue(0);
            entity.Property(e => e.PhoneNormalized).HasMaxLength(20);
            entity.Property(e => e.PhoneRaw).HasMaxLength(200);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.Campaign).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MessageHistory>(entity =>
        {
            entity.ToTable("MessageHistory");

            entity.HasIndex(e => e.CampaignId, "IX_MessageHistory_CampaignId");

            entity.HasIndex(e => e.CampaignId1, "IX_MessageHistory_CampaignId1");

            entity.HasIndex(e => new { e.UserId, e.SentAt }, "IX_MessageHistory_UserId_SentAt");

            entity.Property(e => e.Cost).HasPrecision(10, 4);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Campaign).WithMany(p => p.MessageHistories).HasForeignKey(d => d.CampaignId);

            entity.HasOne(d => d.CampaignId1Navigation).WithMany(p => p.MessageHistories).HasForeignKey(d => d.CampaignId1);

            entity.HasOne(d => d.User).WithMany(p => p.MessageHistories).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<MessageTimingControl>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_MessageTimingControls_IsActive");

            entity.HasIndex(e => e.SubscriptionPlanId, "IX_MessageTimingControls_SubscriptionPlanId");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.MessageTimingControls)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasIndex(e => e.SubscriptionId, "IX_PaymentTransactions_SubscriptionId");

            entity.HasIndex(e => e.UserId, "IX_PaymentTransactions_UserId");

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Subscription).WithMany(p => p.PaymentTransactions).HasForeignKey(d => d.SubscriptionId);

            entity.HasOne(d => d.User).WithMany(p => p.PaymentTransactions).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<RandomDelayRule>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_RandomDelayRules_IsActive");

            entity.HasIndex(e => e.Priority, "IX_RandomDelayRules_Priority");

            entity.HasIndex(e => e.SubscriptionPlanId, "IX_RandomDelayRules_SubscriptionPlanId");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.RandomDelayRules)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SentPhone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SentPhones_pkey");

            entity.HasIndex(e => e.CampaignId, "IDX_SentPhones_CampaignId");

            entity.HasIndex(e => e.UserId, "IDX_SentPhones_UserId");

            entity.HasIndex(e => new { e.UserId, e.PhoneNumber }, "IDX_SentPhones_UserPhone").IsUnique();

            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'sent'::character varying");
        });

        modelBuilder.Entity<SentPhoneNumber>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SentPhoneNumbers_pkey");

            entity.HasIndex(e => e.PhoneNumber, "IX_SentPhoneNumbers_PhoneNumber");

            entity.HasIndex(e => new { e.UserId, e.PhoneNumber }, "IX_SentPhoneNumbers_UserId_PhoneNumber").IsUnique();

            entity.Property(e => e.FirstSentAt)
                .HasDefaultValueSql("(now() AT TIME ZONE 'utc'::text)")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.LastSentAt)
                .HasDefaultValueSql("(now() AT TIME ZONE 'utc'::text)")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.LastStatus).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(100);
            entity.Property(e => e.SendCount).HasDefaultValue(1);
            entity.Property(e => e.UserId).HasMaxLength(450);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasIndex(e => e.Key, "IX_SystemSettings_Key").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Key).HasMaxLength(100);
        });

        modelBuilder.Entity<TemplateAttachment>(entity =>
        {
            entity.HasIndex(e => e.CampaignTemplateId, "IX_TemplateAttachments_CampaignTemplateId");

            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(255);

            entity.HasOne(d => d.CampaignTemplate).WithMany(p => p.TemplateAttachments).HasForeignKey(d => d.CampaignTemplateId);
        });

        modelBuilder.Entity<UsageStatistic>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Date }, "IX_UsageStatistics_UserId_Date").IsUnique();

            entity.Property(e => e.TotalCost).HasPrecision(10, 4);

            entity.HasOne(d => d.User).WithMany(p => p.UsageStatistics).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasIndex(e => e.SubscriptionPlanId, "IX_UserSubscriptions_SubscriptionPlanId");

            entity.HasIndex(e => e.UserId, "IX_UserSubscriptions_UserId");

            entity.Property(e => e.AmountPaid).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.UserSubscriptions)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.User).WithMany(p => p.UserSubscriptions).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<VideoTimingControl>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_VideoTimingControls_IsActive");

            entity.HasIndex(e => e.SubscriptionPlanId, "IX_VideoTimingControls_SubscriptionPlanId");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.VideoTimingControls)
                .HasForeignKey(d => d.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
