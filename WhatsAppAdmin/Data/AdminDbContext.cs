using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Data
{
    /// <summary>
    /// Database context for the WhatsApp Admin module
    /// Manages all admin entity relationships and database operations
    /// </summary>
    public class AdminDbContext : IdentityDbContext<AdminUser>
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
        {
        }

        // Admin models
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<SubscriptionPermission> SubscriptionPermissions { get; set; }
        public DbSet<TimerSettings> TimerSettings { get; set; }
        public DbSet<CampaignTemplate> CampaignTemplates { get; set; }
        public DbSet<TemplateAttachment> TemplateAttachments { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<MessageHistory> MessageHistories { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<UsageStatistic> UsageStatistics { get; set; }

        // Timing control models
        public DbSet<MessageTimingControl> MessageTimingControls { get; set; }
        public DbSet<RandomDelayRule> RandomDelayRules { get; set; }
        public DbSet<VideoTimingControl> VideoTimingControls { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure SubscriptionPermission junction table
            builder.Entity<SubscriptionPermission>()
                .HasKey(sp => new { sp.SubscriptionId, sp.PermissionId });

            builder.Entity<SubscriptionPermission>()
                .HasOne(sp => sp.Subscription)
                .WithMany(s => s.SubscriptionPermissions)
                .HasForeignKey(sp => sp.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SubscriptionPermission>()
                .HasOne(sp => sp.Permission)
                .WithMany(p => p.SubscriptionPermissions)
                .HasForeignKey(sp => sp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure TimerSettings relationship
            builder.Entity<TimerSettings>()
                .HasOne(ts => ts.Subscription)
                .WithOne(s => s.TimerSettings)
                .HasForeignKey<TimerSettings>(ts => ts.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure UserSubscription relationship
            builder.Entity<UserSubscription>()
                .HasOne(us => us.Subscription)
                .WithMany(s => s.UserSubscriptions)
                .HasForeignKey(us => us.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserSubscription>()
                .Property(us => us.AmountPaid)
                .HasPrecision(18, 2);

            // Add indexes for performance
            builder.Entity<UserSubscription>()
                .HasIndex(us => us.UserId);

            builder.Entity<UserSubscription>()
                .HasIndex(us => us.UserEmail);

            builder.Entity<Permission>()
                .HasIndex(p => p.Name)
                .IsUnique();

            builder.Entity<Subscription>()
                .HasIndex(s => s.Name)
                .IsUnique();

            // Configure decimal precision for Price field
            builder.Entity<Subscription>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            // Configure TemplateAttachment relationship
            builder.Entity<TemplateAttachment>()
                .HasOne(ta => ta.CampaignTemplate)
                .WithMany(ct => ct.Attachments)
                .HasForeignKey(ta => ta.CampaignTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure additional models
            builder.Entity<ApiKey>(b =>
            {
                b.HasIndex(e => e.Key).IsUnique();
                b.HasIndex(e => new { e.UserId, e.IsActive });
            });

            builder.Entity<MessageHistory>(b =>
            {
                b.HasIndex(e => e.CampaignId);
                b.HasIndex(e => new { e.UserId, e.SentAt });
            });

            builder.Entity<PaymentTransaction>(b =>
            {
                b.HasIndex(e => e.SubscriptionId);
                b.HasIndex(e => e.UserId);
            });

            builder.Entity<SystemSetting>(b =>
            {
                b.HasIndex(e => e.Key).IsUnique();
            });

            builder.Entity<UsageStatistic>(b =>
            {
                b.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
            });

            // Configure timing control models
            builder.Entity<MessageTimingControl>(b =>
            {
                b.HasIndex(e => e.SubscriptionId);
                b.HasIndex(e => e.IsActive);
            });

            builder.Entity<RandomDelayRule>(b =>
            {
                b.HasIndex(e => e.SubscriptionId);
                b.HasIndex(e => e.IsActive);
                b.HasIndex(e => e.Priority);
            });

            builder.Entity<VideoTimingControl>(b =>
            {
                b.HasIndex(e => e.SubscriptionId);
                b.HasIndex(e => e.IsActive);
            });

            // Configure admin roles
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
                new IdentityRole { Id = "2", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "3", Name = "SemiAdmin", NormalizedName = "SEMIADMIN" }
            );
        }
    }
}
