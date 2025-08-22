
using Microsoft.AspNetCore.Identity;
using EquityPerformanceTracker.Core.Models;
using EquityPerformanceTracker.Data.Context;

namespace EquityPerformanceTracker.Services
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            try
            {
                // Ensure database is created
                await context.Database.EnsureCreatedAsync();

                // Seed roles
                await SeedRolesAsync(roleManager);

                // Seed admin user
                await SeedAdminUserAsync(userManager);

                // Seed sample data for development
                if (context.Portfolios.Any() == false)
                {
                    await SeedSampleDataAsync(context, userManager);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                Console.WriteLine($"Error seeding database: {ex.Message}");
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "Admin", "User", "Premium" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            var adminEmail = "admin@equitytracker.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow,
                    LastLoginDate = DateTime.UtcNow,
                    IsSubscribed = true,
                    SubscriptionStartDate = DateTime.UtcNow,
                    SubscriptionEndDate = DateTime.UtcNow.AddYears(10), // Long-term admin access
                    IsTrialActive = false
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        private static async Task SeedSampleDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // Create a sample user
            var sampleEmail = "demo@equitytracker.com";
            var sampleUser = await userManager.FindByEmailAsync(sampleEmail);

            if (sampleUser == null)
            {
                sampleUser = new ApplicationUser
                {
                    UserName = sampleEmail,
                    Email = sampleEmail,
                    FirstName = "Demo",
                    LastName = "User",
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow,
                    LastLoginDate = DateTime.UtcNow,
                    IsSubscribed = false,
                    IsTrialActive = true,
                    TrialEndDate = DateTime.UtcNow.AddDays(14)
                };

                var result = await userManager.CreateAsync(sampleUser, "Demo123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(sampleUser, "User");

                    // Create sample portfolio for demo user
                    var samplePortfolio = new Portfolio
                    {
                        UserId = sampleUser.Id,
                        Name = "My First Portfolio",
                        Description = "Sample portfolio for demonstration",
                        InitialValue = 10000m,
                        CurrentValue = 10000m,
                        CreatedDate = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    };

                    context.Portfolios.Add(samplePortfolio);
                    await context.SaveChangesAsync();

                    // Add sample holdings
                    var sampleHoldings = new List<PortfolioHolding>
                    {
                        new PortfolioHolding
                        {
                            PortfolioId = samplePortfolio.Id,
                            Symbol = "AAPL",
                            CompanyName = "Apple Inc.",
                            Shares = 10,
                            PurchasePrice = 150.00m,
                            CurrentPrice = 150.00m,
                            PurchaseDate = DateTime.UtcNow.AddDays(-30),
                            LastPriceUpdate = DateTime.UtcNow
                        },
                        new PortfolioHolding
                        {
                            PortfolioId = samplePortfolio.Id,
                            Symbol = "MSFT",
                            CompanyName = "Microsoft Corporation",
                            Shares = 15,
                            PurchasePrice = 300.00m,
                            CurrentPrice = 300.00m,
                            PurchaseDate = DateTime.UtcNow.AddDays(-25),
                            LastPriceUpdate = DateTime.UtcNow
                        },
                        new PortfolioHolding
                        {
                            PortfolioId = samplePortfolio.Id,
                            Symbol = "GOOGL",
                            CompanyName = "Alphabet Inc.",
                            Shares = 5,
                            PurchasePrice = 100.00m,
                            CurrentPrice = 100.00m,
                            PurchaseDate = DateTime.UtcNow.AddDays(-20),
                            LastPriceUpdate = DateTime.UtcNow
                        }
                    };

                    context.PortfolioHoldings.AddRange(sampleHoldings);
                    await context.SaveChangesAsync();

                    // Update portfolio current value
                    samplePortfolio.CurrentValue = sampleHoldings.Sum(h => h.TotalValue);
                    context.Portfolios.Update(samplePortfolio);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}