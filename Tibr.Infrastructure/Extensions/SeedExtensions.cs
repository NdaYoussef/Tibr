using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tibr.Infrastructure.Contexts;
using Tibr.Infrastructure.Exceptions;

namespace Tibr.Infrastructure.Extensions
{
    public static class SeedExtensions
    {
        public static async Task SeedDatabaseAsync(this IApplicationBuilder app)
        {
            var logger = app.ApplicationServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("SeedDatabase");

            try
            {
                logger.LogInformation("Database seeding started...");

                await using var scope = app.ApplicationServices.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                try
                {
                    logger.LogInformation("Applying pending migrations...");
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied.");

                    logger.LogInformation("Seeding super admin data...");
                    await Seeds.SeedData.SeedSuperAdminAsync(context);
                    logger.LogInformation("Database seeding completed successfully.");
                }
                catch (SeedDataException seedEx)
                {
                    logger.LogError(seedEx, "Seed data error: {Message}", seedEx.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error during database seeding.");
                    throw new SeedDataException("Database seeding failed. See inner exception for details.", ex);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Critical error during database seeding.");
                throw;
            }
        }

       
    }
}