using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhatsAppAdmin.Data;
using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Data.Seeders
{
    /// <summary>
    /// Master seeder that coordinates all data seeding operations
    /// Ensures proper order of seeding with dependencies
    /// </summary>
    public static class MasterSeeder
    {
        /// <summary>
        /// This method is kept for compatibility but redirects to SeedAdminOnlyAsync
        /// API keys are seeded separately in the WhatsAppSender.API project
        /// </summary>
        public static async Task SeedAllAsync(
            UserManager<AdminUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AdminDbContext adminContext)
        {
            await SeedAdminOnlyAsync(userManager, roleManager, adminContext);
        }

        private static async Task EnsureSubscriptionsExist(AdminDbContext context)
        {
            var subscriptionCount = await context.Subscriptions.CountAsync();
            if (subscriptionCount == 0)
            {
                Console.WriteLine("⚠️ No subscriptions found. Make sure subscription seeding is configured in the DbContext.");
            }
            else
            {
                Console.WriteLine($"ℹ️ Found {subscriptionCount} subscription plans available");
            }
        }


        /// <summary>
        /// Comprehensive admin seeding method with all data types
        /// </summary>
        public static async Task SeedAdminOnlyAsync(
            UserManager<AdminUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AdminDbContext adminContext)
        {
            Console.WriteLine("🌱 Starting comprehensive admin data seeding...\n");

            try
            {
                // 1. Seed Admin Users and Roles
                Console.WriteLine("👥 Seeding Admin Users and Roles...");
                await UserSeeder.SeedUsersAsync(userManager, roleManager);

                // 2. Seed Subscription Plans
                Console.WriteLine("\n📦 Seeding Subscription Plans...");
                await SubscriptionSeeder.SeedSubscriptionsAsync(adminContext);

                // 3. Seed Campaign Templates
                Console.WriteLine("\n📧 Seeding Campaign Templates...");
                await CampaignTemplateSeeder.SeedCampaignTemplatesAsync(adminContext, userManager);

                // 4. Seed User Subscription Assignments
                Console.WriteLine("\n📋 Seeding User Subscription Assignments...");
                await UserSubscriptionSeeder.SeedUserSubscriptionsAsync(adminContext);

                // 5. Display comprehensive summary
                await DisplayComprehensiveSummary(adminContext);

                Console.WriteLine("\n✅ Comprehensive admin data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error during seeding: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private static async Task DisplayAdminSummary(AdminDbContext adminContext)
        {
            Console.WriteLine("\n📊 === ADMIN SEEDING SUMMARY ===");

            // Admin Users Summary
            var totalUsers = await adminContext.Users.CountAsync();
            var activeUsers = await adminContext.Users.CountAsync(u => u.IsActive);
            Console.WriteLine($"👥 Admin Users: {totalUsers} total ({activeUsers} active)");

            // User Subscriptions Summary
            var totalAssignments = await adminContext.UserSubscriptions.CountAsync();
            var activeAssignments = await adminContext.UserSubscriptions.CountAsync(us => us.IsActive);
            Console.WriteLine($"📋 User Assignments: {totalAssignments} total ({activeAssignments} active)");

            // Subscription breakdown
            var subscriptionBreakdown = await adminContext.UserSubscriptions
                .Include(us => us.Subscription)
                .Where(us => us.IsActive)
                .GroupBy(us => us.Subscription.Name)
                .Select(g => new { Plan = g.Key, Count = g.Count() })
                .ToListAsync();

            Console.WriteLine("   Breakdown by plan:");
            foreach (var item in subscriptionBreakdown)
            {
                Console.WriteLine($"     - {item.Plan}: {item.Count} users");
            }

            Console.WriteLine("============================");
        }

        private static async Task DisplayComprehensiveSummary(AdminDbContext adminContext)
        {
            Console.WriteLine("\n📊 === COMPREHENSIVE SEEDING SUMMARY ===");

            // Admin Users Summary
            var totalUsers = await adminContext.Users.CountAsync();
            var activeUsers = await adminContext.Users.CountAsync(u => u.IsActive);
            Console.WriteLine($"👥 Admin Users: {totalUsers} total ({activeUsers} active)");

            // Subscription Plans Summary
            var totalSubscriptions = await adminContext.Subscriptions.CountAsync();
            var activeSubscriptions = await adminContext.Subscriptions.CountAsync(s => s.IsActive);
            var avgPrice = await adminContext.Subscriptions.Where(s => s.Price > 0).AverageAsync(s => (double)s.Price);
            Console.WriteLine($"📦 Subscription Plans: {totalSubscriptions} total ({activeSubscriptions} active), avg price AED {avgPrice:F2}");

            // Campaign Templates Summary
            var totalTemplates = await adminContext.CampaignTemplates.CountAsync();
            var globalTemplates = await adminContext.CampaignTemplates.CountAsync(ct => ct.IsGlobal);
            var avgUsage = await adminContext.CampaignTemplates.AverageAsync(ct => (double)ct.TimesUsed);
            Console.WriteLine($"📧 Campaign Templates: {totalTemplates} total ({globalTemplates} global), avg usage {avgUsage:F1} times");

            // User Subscriptions Summary
            var totalAssignments = await adminContext.UserSubscriptions.CountAsync();
            var activeAssignments = await adminContext.UserSubscriptions.CountAsync(us => us.IsActive);
            var totalDailyUsage = await adminContext.UserSubscriptions.Where(us => us.IsActive).SumAsync(us => us.MessagesUsedToday);
            Console.WriteLine($"📋 User Assignments: {totalAssignments} total ({activeAssignments} active), {totalDailyUsage:N0} messages used today");

            // Subscription breakdown with usage
            var subscriptionBreakdown = await adminContext.UserSubscriptions
                .Include(us => us.Subscription)
                .Where(us => us.IsActive)
                .GroupBy(us => us.Subscription.Name)
                .Select(g => new {
                    Plan = g.Key,
                    Count = g.Count(),
                    Usage = g.Sum(us => us.MessagesUsedToday)
                })
                .OrderByDescending(x => x.Usage)
                .ToListAsync();

            Console.WriteLine("\n   📈 Active Subscriptions by Usage:");
            foreach (var item in subscriptionBreakdown)
            {
                Console.WriteLine($"     - {item.Plan}: {item.Count} users, {item.Usage:N0} messages today");
            }

            // High usage alerts
            var heavyUsers = await adminContext.UserSubscriptions
                .Include(us => us.Subscription)
                .Where(us => us.IsActive && us.MessagesUsedToday > 1000)
                .CountAsync();

            if (heavyUsers > 0)
            {
                Console.WriteLine($"\n   ⚠️  High Usage Alert: {heavyUsers} users with >1000 messages today");
            }

            // Expiring subscriptions
            var expiringSoon = await adminContext.UserSubscriptions
                .Where(us => us.IsActive && us.ExpiresAt <= DateTime.UtcNow.AddDays(30))
                .CountAsync();

            if (expiringSoon > 0)
            {
                Console.WriteLine($"   📅 Renewal Alert: {expiringSoon} subscriptions expiring within 30 days");
            }

            Console.WriteLine("==========================================");
        }

        /// <summary>
        /// Quick seeding method for development/testing
        /// </summary>
        public static async Task SeedDevelopmentDataAsync(
            UserManager<AdminUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AdminDbContext adminContext)
        {
            Console.WriteLine("🚀 Quick seeding for development environment...");

            // Create minimal data for development
            await UserSeeder.SeedUsersAsync(userManager, roleManager);

            // Add a few test user assignments
            if (!await adminContext.UserSubscriptions.AnyAsync())
            {
                var basicSubscription = await adminContext.Subscriptions.FirstOrDefaultAsync(s => s.Name == "Basic Plan");
                if (basicSubscription != null)
                {
                    var devAssignments = new List<UserSubscription>
                    {
                        new UserSubscription
                        {
                            UserId = "99",
                            UserEmail = "developer@test.com",
                            SubscriptionId = basicSubscription.Id,
                            AssignedAt = DateTime.UtcNow,
                            ExpiresAt = DateTime.UtcNow.AddDays(365),
                            IsActive = true,
                            MessagesUsedToday = 5,
                            LastResetAt = DateTime.UtcNow.Date
                        }
                    };

                    await adminContext.UserSubscriptions.AddRangeAsync(devAssignments);
                    await adminContext.SaveChangesAsync();
                    Console.WriteLine("✅ Added development user assignments");
                }
            }

            Console.WriteLine("✅ Development seeding completed");
        }
    }
}