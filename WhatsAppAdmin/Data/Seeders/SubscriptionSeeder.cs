using Microsoft.EntityFrameworkCore;
using WhatsAppAdmin.Data;
using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Data.Seeders
{
    /// <summary>
    /// Seeds comprehensive subscription plans for the WhatsApp Admin system
    /// Provides realistic UAE business-focused subscription tiers
    /// </summary>
    public static class SubscriptionSeeder
    {
        public static async Task SeedSubscriptionsAsync(AdminDbContext context)
        {
            // Check if subscriptions already exist
            if (await context.Subscriptions.AnyAsync())
            {
                Console.WriteLine("ℹ️ Subscriptions already exist, skipping seeding");
                return;
            }

            Console.WriteLine("📦 Seeding subscription plans...");

            var subscriptions = new List<Subscription>
            {
                // Free Trial - Perfect for testing
                new Subscription
                {
                    Name = "Free Trial",
                    Description = "30-day free trial with limited features. Perfect for testing our WhatsApp messaging platform.",
                    MaxMessagesPerDay = 25,
                    Price = 0.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    UpdatedAt = DateTime.UtcNow.AddDays(-90)
                },

                // Starter Plan - Small businesses
                new Subscription
                {
                    Name = "Basic Plan",
                    Description = "Ideal for small UAE businesses and startups. Includes essential WhatsApp messaging features.",
                    MaxMessagesPerDay = 100,
                    Price = 49.00m, // AED pricing
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                },

                // Professional Plan - Growing businesses
                new Subscription
                {
                    Name = "Professional Plan",
                    Description = "For growing UAE enterprises with moderate messaging needs. Includes advanced analytics and priority support.",
                    MaxMessagesPerDay = 500,
                    Price = 149.00m, // AED pricing
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    UpdatedAt = DateTime.UtcNow.AddDays(-15)
                },

                // Business Plan - Large companies
                new Subscription
                {
                    Name = "Business Plan",
                    Description = "Perfect for established UAE businesses with high-volume messaging requirements. Includes custom integrations.",
                    MaxMessagesPerDay = 1500,
                    Price = 399.00m, // AED pricing
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                },

                // Enterprise Plan - Large corporations
                new Subscription
                {
                    Name = "Enterprise Plan",
                    Description = "Unlimited messaging for large UAE corporations and government entities. White-label solutions available.",
                    MaxMessagesPerDay = 5000,
                    Price = 999.00m, // AED pricing
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },

                // Premium Enterprise - For very large operations
                new Subscription
                {
                    Name = "Premium Enterprise",
                    Description = "Ultimate solution for multinational companies operating in UAE. Dedicated infrastructure and 24/7 support.",
                    MaxMessagesPerDay = 10000,
                    Price = 2499.00m, // AED pricing
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },

                // Legacy Plan - Discontinued but existing customers
                new Subscription
                {
                    Name = "Legacy Starter",
                    Description = "Discontinued plan for existing customers. No longer available for new subscriptions.",
                    MaxMessagesPerDay = 50,
                    Price = 29.00m, // AED pricing
                    IsActive = false, // Inactive for new subscriptions
                    CreatedAt = DateTime.UtcNow.AddDays(-180),
                    UpdatedAt = DateTime.UtcNow.AddDays(-60)
                },

                // Custom Plan - For special agreements
                new Subscription
                {
                    Name = "Custom Enterprise",
                    Description = "Tailored solution for organizations with specific requirements. Contact sales for pricing and features.",
                    MaxMessagesPerDay = 25000,
                    Price = 4999.00m, // AED pricing
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-45),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            await context.Subscriptions.AddRangeAsync(subscriptions);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Seeded {subscriptions.Count} subscription plans");

            // Display summary
            var activeCount = subscriptions.Count(s => s.IsActive);
            var inactiveCount = subscriptions.Count - activeCount;
            var avgPrice = subscriptions.Where(s => s.Price > 0).Average(s => s.Price);

            Console.WriteLine("📊 Subscription Summary:");
            Console.WriteLine($"   - Total Plans: {subscriptions.Count}");
            Console.WriteLine($"   - Active Plans: {activeCount}");
            Console.WriteLine($"   - Inactive Plans: {inactiveCount}");
            Console.WriteLine($"   - Average Price: AED {avgPrice:F2}");
            Console.WriteLine($"   - Price Range: Free - AED {subscriptions.Max(s => s.Price):F2}");

            Console.WriteLine("\n💰 Active Plans Available:");
            foreach (var sub in subscriptions.Where(s => s.IsActive).OrderBy(s => s.Price))
            {
                Console.WriteLine($"   - {sub.Name}: {sub.MaxMessagesPerDay} msgs/day @ AED {sub.Price:F2}/month");
            }
        }
    }
}