using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using System.Security.Cryptography;

namespace WhatsAppAdmin.Data.Seeders
{
    /// <summary>
    /// Seeds WhatsApp API keys for the shared SaaS database
    /// Creates realistic API keys linked to user subscriptions
    /// </summary>
    public static class WhatsAppApiKeySeeder
    {
        public static async Task SeedWhatsAppApiKeysAsync(SaaSDbContext context)
        {
            // Check if API keys already exist
            if (await context.ApiKeys.AnyAsync())
            {
                Console.WriteLine("ℹ️ WhatsApp API keys already exist, skipping seeding");
                return;
            }

            Console.WriteLine("🔑 Seeding WhatsApp API keys...");

            // Get subscription plans and users
            var subscriptions = await context.SubscriptionPlans.ToListAsync();
            var users = await context.Users.ToListAsync();

            if (!subscriptions.Any())
            {
                Console.WriteLine("⚠️ No subscription plans found, cannot seed API keys");
                return;
            }

            if (!users.Any())
            {
                Console.WriteLine("⚠️ No users found, creating demo users for API keys");
                await SeedDemoUsersAsync(context);
                users = await context.Users.ToListAsync();
            }

            // Get subscription plans
            var starterPlan = subscriptions.FirstOrDefault(s => s.Name == "Starter");
            var professionalPlan = subscriptions.FirstOrDefault(s => s.Name == "Professional");
            var businessPlan = subscriptions.FirstOrDefault(s => s.Name == "Business");
            var enterprisePlan = subscriptions.FirstOrDefault(s => s.Name == "Enterprise");

            if (starterPlan == null || professionalPlan == null || businessPlan == null || enterprisePlan == null)
            {
                Console.WriteLine("⚠️ Required subscription plans not found");
                return;
            }

            var apiKeys = new List<WhatsApp.Shared.Models.ApiKey>
            {
                // Enterprise Users - Multiple API Keys
                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Production WhatsApp API",
                    SubscriptionPlanId = enterprisePlan.Id,
                    DailyQuotaUsed = 3450,
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-5),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-90)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Development & Testing API",
                    SubscriptionPlanId = enterprisePlan.Id,
                    DailyQuotaUsed = 125,
                    LastUsedAt = DateTime.UtcNow.AddHours(-2),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-75)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(1).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Marketing Campaigns API",
                    SubscriptionPlanId = enterprisePlan.Id,
                    DailyQuotaUsed = 7800,
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-2),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-120)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(1).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Customer Support Bot",
                    SubscriptionPlanId = enterprisePlan.Id,
                    DailyQuotaUsed = 2340,
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-15),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                },

                // Business Plan Users - High Volume
                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(2).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "E-commerce Notifications",
                    SubscriptionPlanId = businessPlan.Id,
                    DailyQuotaUsed = 1650,
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-20),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-45)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(2).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Order Status Updates",
                    SubscriptionPlanId = businessPlan.Id,
                    DailyQuotaUsed = 890,
                    LastUsedAt = DateTime.UtcNow.AddHours(-1),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(3).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Real Estate Leads",
                    SubscriptionPlanId = businessPlan.Id,
                    DailyQuotaUsed = 1230,
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-35),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-80)
                },

                // Professional Plan Users - Growing Businesses
                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(4).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Restaurant Booking System",
                    SubscriptionPlanId = professionalPlan.Id,
                    DailyQuotaUsed = 345,
                    LastUsedAt = DateTime.UtcNow.AddHours(-3),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(4).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Appointment Reminders",
                    SubscriptionPlanId = professionalPlan.Id,
                    DailyQuotaUsed = 178,
                    LastUsedAt = DateTime.UtcNow.AddHours(-5),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-40)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(5).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Fitness Class Bookings",
                    SubscriptionPlanId = professionalPlan.Id,
                    DailyQuotaUsed = 267,
                    LastUsedAt = DateTime.UtcNow.AddHours(-4),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-35)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(6).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Property Management",
                    SubscriptionPlanId = professionalPlan.Id,
                    DailyQuotaUsed = 456,
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-90),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-55)
                },

                // Starter Plan Users - Small Businesses
                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(7).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Local Cafe Updates",
                    SubscriptionPlanId = starterPlan.Id,
                    DailyQuotaUsed = 45,
                    LastUsedAt = DateTime.UtcNow.AddHours(-6),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(8).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Barber Shop Appointments",
                    SubscriptionPlanId = starterPlan.Id,
                    DailyQuotaUsed = 67,
                    LastUsedAt = DateTime.UtcNow.AddHours(-8),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(9).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Boutique Store Promotions",
                    SubscriptionPlanId = starterPlan.Id,
                    DailyQuotaUsed = 23,
                    LastUsedAt = DateTime.UtcNow.AddHours(-12),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(10).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Taxi Service Booking",
                    SubscriptionPlanId = starterPlan.Id,
                    DailyQuotaUsed = 89,
                    LastUsedAt = DateTime.UtcNow.AddHours(-2),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-18)
                },

                // Heavy Usage - Near Limits
                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(11).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Bulk Marketing Campaign",
                    SubscriptionPlanId = enterprisePlan.Id,
                    DailyQuotaUsed = 9850, // Near enterprise limit
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-1),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-100)
                },

                // Inactive/Revoked Keys - For testing
                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(12).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Legacy Integration (Revoked)",
                    SubscriptionPlanId = professionalPlan.Id,
                    DailyQuotaUsed = 0,
                    LastUsedAt = DateTime.UtcNow.AddDays(-45),
                    LastResetAt = DateTime.UtcNow.Date.AddDays(-45),
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-200),
                    RevokedAt = DateTime.UtcNow.AddDays(-45)
                },

                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(13).First().Id.ToString(),
                    Key = GenerateApiKey(),
                    Name = "Expired Trial Key",
                    SubscriptionPlanId = starterPlan.Id,
                    DailyQuotaUsed = 0,
                    LastUsedAt = DateTime.UtcNow.AddDays(-35),
                    LastResetAt = DateTime.UtcNow.Date.AddDays(-35),
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    RevokedAt = DateTime.UtcNow.AddDays(-30)
                },

                // Demo/Testing Keys
                new WhatsApp.Shared.Models.ApiKey
                {
                    UserId = users.Skip(14).First().Id.ToString(),
                    Key = "demo-whatsapp-api-key-12345678901234567890123456789012345678",
                    Name = "Demo API Key",
                    SubscriptionPlanId = professionalPlan.Id,
                    DailyQuotaUsed = 85,
                    LastUsedAt = DateTime.UtcNow.AddHours(-4),
                    LastResetAt = DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            await context.ApiKeys.AddRangeAsync(apiKeys);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Seeded {apiKeys.Count} WhatsApp API keys");

            // Display summary
            var summary = apiKeys
                .GroupBy(ak => ak.SubscriptionPlan?.Name ?? "Unknown")
                .Select(g => new {
                    Plan = g.Key,
                    Count = g.Count(),
                    Active = g.Count(k => k.IsActive),
                    TotalUsage = g.Sum(k => k.DailyQuotaUsed)
                })
                .OrderByDescending(s => s.TotalUsage)
                .ToList();

            Console.WriteLine("📊 API Keys Summary:");
            foreach (var item in summary)
            {
                Console.WriteLine($"   - {item.Plan}: {item.Count} keys ({item.Active} active), {item.TotalUsage:N0} daily usage");
            }

            Console.WriteLine("\n🔑 Sample API Keys for Testing:");
            var sampleKeys = apiKeys.Where(k => k.IsActive).Take(3).ToList();
            foreach (var key in sampleKeys)
            {
                Console.WriteLine($"   - {key.Name}: {key.Key.Substring(0, 16)}... ({key.DailyQuotaUsed} used today)");
            }

            var heavyUsers = apiKeys.Where(k => k.DailyQuotaUsed > 5000).ToList();
            if (heavyUsers.Any())
            {
                Console.WriteLine($"\n⚠️ Heavy Usage Alert: {heavyUsers.Count} keys using >5000 messages/day");
                foreach (var key in heavyUsers)
                {
                    Console.WriteLine($"   - {key.Name}: {key.DailyQuotaUsed:N0} messages today");
                }
            }
        }

        private static async Task SeedDemoUsersAsync(SaaSDbContext context)
        {
            Console.WriteLine("👤 Creating demo users for API key testing...");

            var demoUsers = new List<ApplicationUser>
            {
                new ApplicationUser { UserName = "enterprise1@demo.com", Email = "enterprise1@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "enterprise2@demo.com", Email = "enterprise2@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "business1@demo.com", Email = "business1@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "business2@demo.com", Email = "business2@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "professional1@demo.com", Email = "professional1@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "professional2@demo.com", Email = "professional2@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "professional3@demo.com", Email = "professional3@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "professional4@demo.com", Email = "professional4@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "starter1@demo.com", Email = "starter1@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "starter2@demo.com", Email = "starter2@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "starter3@demo.com", Email = "starter3@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "starter4@demo.com", Email = "starter4@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "heavy@demo.com", Email = "heavy@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "inactive1@demo.com", Email = "inactive1@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "inactive2@demo.com", Email = "inactive2@demo.com", EmailConfirmed = true },
                new ApplicationUser { UserName = "demo@demo.com", Email = "demo@demo.com", EmailConfirmed = true }
            };

            await context.Users.AddRangeAsync(demoUsers);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Created {demoUsers.Count} demo users");
        }

        private static string GenerateApiKey()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[64];
            rng.GetBytes(bytes);

            return "wapi_" + new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }
}