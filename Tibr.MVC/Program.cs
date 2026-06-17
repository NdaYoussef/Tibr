using Mapster;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Tibr.Infrastructure;
using Tibr.Infrastructure.Contexts;
using Tibr.Infrastructure.Seeds;

namespace Tibr.MVC
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            );
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddInfrastructure(builder.Configuration);

            //// Add MediatR for CQRS pattern
            //builder.Services.AddMediatR(cfg =>
            //    cfg.RegisterServicesFromAssembly(typeof(Tibr.Application.Services.Auth.RegisterCommand).Assembly));


            //builder.Services.AddApplicationServices();

            TypeAdapterConfig.GlobalSettings.Scan(
                        typeof(Tibr.Application.Mappers.ProductMappingConfig).Assembly,
                        typeof(Tibr.MVC.Mapping.DashboardMappingConfig).Assembly);

            // Add HttpClientFactory
            builder.Services.AddHttpClient();

            // Add Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/AdminAuth/Login";
                    options.LogoutPath = "/AdminAuth/Logout";
                    options.AccessDeniedPath = "/AdminAuth/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.SlidingExpiration = true;
                });

            var app = builder.Build();

            // Apply Migrations & Seed Data
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();

                    // Apply pending migrations
                    await context.Database.MigrateAsync();

                    // Seed Super Admin
                    await SeedData.SeedSuperAdminAsync(context);

                    Console.WriteLine("Database migration and seeding completed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database initialization error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=AdminAuth}/{action=Login}/{id?}")
                .WithStaticAssets();

            await app.RunAsync();
        }
    }
}