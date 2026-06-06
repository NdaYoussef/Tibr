using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tibr.Domain.Entities;
using Tibr.Infrastructure.Contexts;
using Tibr.Infrastructure.Exceptions;

namespace Tibr.Infrastructure.Seeds
{
    public static class SeedData
    {
        private static ILogger? _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public static void SeedSuperAdmin(ApplicationDbContext context)
        {
            try
            {
                _logger?.LogInformation("Starting super admin seeding...");

                var userExists = context.Users.Any(u => u.Email == "admin@tibr.com");
                var adminExists = context.Admins.Any(a => a.Email == "admin@tibr.com");

                if (userExists && adminExists)
                {
                    _logger?.LogInformation("Super admin already exists. Skipping seed operation.");
                    return;
                }

                if (!userExists)
                {
                    _logger?.LogInformation("Creating super admin user...");
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
                    context.Users.Add(superAdminUser);
                    context.SaveChanges();
                    _logger?.LogInformation($"Super admin user created with ID: {superAdminUser.Id}");
                }

                if (!adminExists)
                {
                    _logger?.LogInformation("Creating admin record...");
                    var superAdmin = new Admin
                    {
                        Name = "Super Administrator",
                        Email = "admin@tibr.com",
                        Status = "Active"
                    };
                    context.Admins.Add(superAdmin);
                    context.SaveChanges();
                    _logger?.LogInformation("Admin record created successfully. Seeding completed.");
                }
            }
            catch (DbUpdateException dbEx)
            {
                var errorMsg = "Database error occurred while seeding super admin.";
                _logger?.LogError(dbEx, errorMsg);
                throw new SeedDatabaseConnectionException(errorMsg, dbEx);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Unexpected error occurred while seeding super admin: {ex.Message}";
                _logger?.LogError(ex, errorMsg);
                throw new SeedAdminCreationException(errorMsg, ex);
            }
        }

        public static async Task SeedSuperAdminAsync(ApplicationDbContext context)
        {
            try
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
            catch (DbUpdateException dbEx)
            {
                var errorMsg = "Database error occurred while seeding super admin (async).";
                _logger?.LogError(dbEx, errorMsg);
                throw new SeedDatabaseConnectionException(errorMsg, dbEx);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Unexpected error occurred while seeding super admin (async): {ex.Message}";
                _logger?.LogError(ex, errorMsg);
                throw new SeedAdminCreationException(errorMsg, ex);
            }
        }
    }
}