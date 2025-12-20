using WhatsAppAdmin.Data;
using WhatsAppAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace WhatsAppAdmin.Data.Seeders
{
    /// <summary>
    /// Seeds user subscription assignments linking external users to subscription plans
    /// </summary>
    public static class UserSubscriptionSeeder
    {
        public static async Task SeedUserSubscriptionsAsync(AdminDbContext context)
        {
            // Check if any user subscriptions already exist
            if (await context.UserSubscriptions.AnyAsync())
            {
                Console.WriteLine("ℹ️ User subscriptions already exist, skipping seeding");
                return;
            }

            // Get available subscriptions
            var subscriptions = await context.Subscriptions.ToListAsync();
            if (!subscriptions.Any())
            {
                Console.WriteLine("⚠️ No subscriptions found, cannot seed user assignments");
                return;
            }

            var userSubscriptions = new List<UserSubscription>
            {
                // Premium Enterprise Users - Large corporations
                new UserSubscription
                {
                    UserId = "100",
                    UserEmail = "communications@dubaimunicipality.gov.ae",
                    SubscriptionId = subscriptions.FirstOrDefault(s => s.Name == "Premium Enterprise")?.Id ??
                                   subscriptions.First(s => s.Name == "Enterprise Plan").Id,
                    AssignedAt = DateTime.UtcNow.AddDays(-180),
                    ExpiresAt = DateTime.UtcNow.AddDays(185),
                    IsActive = true,
                    MessagesUsedToday = 2850,
                    LastMessageSentAt = DateTime.UtcNow.AddMinutes(-5),
                    LastResetAt = DateTime.UtcNow.Date
                },

                new UserSubscription
                {
                    UserId = "101",
                    UserEmail = "marketing@emirates.com",
                    SubscriptionId = subscriptions.FirstOrDefault(s => s.Name == "Custom Enterprise")?.Id ??
                                   subscriptions.First(s => s.Name == "Enterprise Plan").Id,
                    AssignedAt = DateTime.UtcNow.AddDays(-365),
                    ExpiresAt = DateTime.UtcNow.AddDays(0), // Expires today - needs renewal
                    IsActive = true,
                    MessagesUsedToday = 5800,
                    LastMessageSentAt = DateTime.UtcNow.AddMinutes(-2),
                    LastResetAt = DateTime.UtcNow.Date
                },

                // Enterprise Plan Users - Large businesses
                new UserSubscription
                {
                    UserId = "102",
                    UserEmail = "ahmed.ali@techcorp.ae",
                    SubscriptionId = subscriptions.First(s => s.Name == "Enterprise Plan").Id,
                    AssignedAt = DateTime.UtcNow.AddDays(-90),
                    ExpiresAt = DateTime.UtcNow.AddDays(275),
                    IsActive = true,
                    MessagesUsedToday = 1250,
                    LastMessageSentAt = DateTime.UtcNow.AddMinutes(-15),
                    LastResetAt = DateTime.UtcNow.Date
                },

                new UserSubscription
                {
                    UserId = "103",
                    UserEmail = "sara.mohammed@digitalsolutions.com",
                    SubscriptionId = subscriptions.First(s => s.Name == "Enterprise Plan").Id,
                    AssignedAt = DateTime.UtcNow.AddDays(-60),
                    ExpiresAt = DateTime.UtcNow.AddDays(305),
                    IsActive = true,
                    MessagesUsedToday = 1680,
                    LastMessageSentAt = DateTime.UtcNow.AddMinutes(-30),
                    LastResetAt = DateTime.UtcNow.Date
                },

                new UserSubscription
                {
                    UserId = "104",
                    UserEmail = "khalid.ibrahim@futuretech.ae",
                    SubscriptionId = subscriptions.First(s => s.Name == "Enterprise Plan").Id,
                    AssignedAt = DateTime.UtcNow.AddDays(-120),
                    ExpiresAt = DateTime.UtcNow.AddDays(245),
                    IsActive = true,
                    MessagesUsedToday = 2100,
                    LastMessageSentAt = DateTime.UtcNow.AddMinutes(-8),
                    LastResetAt = DateTime.UtcNow.Date
                },

                // Business Plan Users - Mid-size companies
                new UserSubscription
                {
                    UserId = "105",
                    UserEmail = "operations@alghuraircentre.ae",
                    SubscriptionId = subscriptions.FirstOrDefault(s => s.Name == "Business Plan")?.Id ??
                                   subscriptions.First(s => s.Name == "Professional Plan").Id,
                    AssignedAt = DateTime.UtcNow.AddDays(-75),
                    ExpiresAt = DateTime.UtcNow.AddDays(290),
                    IsActive = true,
                    MessagesUsedToday = 678,
                    LastMessageSentAt = DateTime.UtcNow.AddMinutes(-45),
                    LastResetAt = DateTime.UtcNow.Date
                },

              
            };

            await context.UserSubscriptions.AddRangeAsync(userSubscriptions);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Seeded {userSubscriptions.Count} user subscription assignments");

            // Display summary
            var summary = userSubscriptions
                .GroupBy(us => us.Subscription?.Name ?? "Unknown")
                .Select(g => new { Plan = g.Key, Count = g.Count() })
                .ToList();

            Console.WriteLine("📊 User Assignment Summary:");
            foreach (var item in summary)
            {
                Console.WriteLine($"   - {item.Plan}: {item.Count} users");
            }
        }
    }
}