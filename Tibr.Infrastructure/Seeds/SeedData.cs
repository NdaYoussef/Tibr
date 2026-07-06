
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Infrastructure.Contexts;
using Tibr.Infrastructure.Exceptions;
using Tibr.Infrastructure.Seed;


namespace Tibr.Infrastructure.Seeds
{
    public static class SeedData
    {
        private static ILogger? _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }


        public static async Task SeedSuperAdminAsync(ApplicationDbContext context)
        {
            _logger?.LogInformation("Starting async super admin seeding...");

            var userExists = await context.Users.AnyAsync(u => u.Email == "admin@tibr.com");
            var adminExists = await context.Admins.AnyAsync(a => a.Email == "admin@tibr.com");

            if (userExists && adminExists)
            {
                _logger?.LogInformation("Super admin already exists. Skipping seed operation.");
                return;
            }

            if (!userExists)
            {
                _logger?.LogInformation("Creating super admin user (async)...");
                var superAdminPassword = BCrypt.Net.BCrypt.HashPassword("SuperAdmin@123");
                var superAdminUser = new User
                {
                    FirstName = "Super",
                    LastName = "Admin",
                    Email = "admin@tibr.com",
                    Phone = "+1-000-000-0000",
                    Password = superAdminPassword,
                    Status = "Active",
                    OtpVerified = true,
                    KycStatus = "Verified",
                    OtpCode = null,
                    OtpExpiry = null
                };
                await context.Users.AddAsync(superAdminUser);
                await context.SaveChangesAsync();
                _logger?.LogInformation($"Super admin user created with ID: {superAdminUser.Id}");
            }

            if (!adminExists)
            {
                _logger?.LogInformation("Creating admin record (async)...");
                var superAdmin = new Admin
                {
                    Name = "Super Administrator",
                    Email = "admin@tibr.com",
                    Status = "Active"
                };
                await context.Admins.AddAsync(superAdmin);
                await context.SaveChangesAsync();
                _logger?.LogInformation("Admin record created successfully. Async seeding completed.");
            }

            await SeedProductsAsync(context);
        }

        public static async Task SeedProductsAsync(ApplicationDbContext context)
        {
            var existingCategories = await context.Categories.Select(c => c.Name).ToListAsync();
            var existingProductNames = await context.Products.Select(p => p.Name).ToListAsync();

            var newCategories = ProductCatalog.Categories
                .Where(c => !existingCategories.Contains(c))
                .Select(c => new Category { Name = c, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, IsDeleted = false })
                .ToList();

            if (newCategories.Count != 0)
            {
                context.Categories.AddRange(newCategories);
                await context.SaveChangesAsync();
            }

            var allCategories = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

            var newProducts = ProductCatalog.Products
                .Where(p => !existingProductNames.Contains(p.Name))
                .Select(p => new Product
                {
                    CategoryId = allCategories[p.Category],
                    Name = p.Name,
                    MetalType = p.Metal,
                    Purity = p.Purity,
                    Weight = p.Weight,
                    BuyPrice = p.BuyPrice,
                    SellPrice = p.SellPrice,
                    Status = ProductStatus.Active,
                    Stock = p.Stock,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                })
                .ToList();

            if (newProducts.Count != 0)
            {
                context.Products.AddRange(newProducts);
                await context.SaveChangesAsync();
                _logger?.LogInformation("Seeded {Count} products across {CatCount} categories.", newProducts.Count, newCategories.Count);
            }
        }

    }
}
