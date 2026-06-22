using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tibr.Domain.Entities;
using Tibr.Infrastructure.Contexts;

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
        }

    }
}