using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Tibr.Application.Interfaces;
using Tibr.Application.Services.Email;
using Tibr.Infrastructure.Contexts;
using Tibr.Application;
using Tibr.Application.Services.PaymentServices;
using Tibr.Infrastructure;
using Tibr.Infrastructure.Config;
using Tibr.Infrastructure.Services;
using Tibr.Infrastructure.Extensions;

namespace Tibr.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
            builder.Services.AddControllers();

            var configuration = builder.Configuration;

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            );

            builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            builder.Services.AddTransient<IEmailService, EmailService>();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = configuration["JWT:ValidAudience"],
                    ValidIssuer = configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!)),
                    ValidateLifetime = true
                };
            });

            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(Tibr.Application.Services.Auth.RegisterCommand).Assembly));

            builder.Services.AddOpenApi();

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplicationServices();

            builder.Services.Configure<PaymobSettings>(
                configuration.GetSection(PaymobSettings.SectionName)
            );
            builder.Services.AddHttpClient<IPaymobService, PaymobService>();

            var app = builder.Build();

            // Seed database with initial data
            try
            {
                app.SeedDatabase();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database. The application may not function correctly.");
                logger.LogWarning("Continuing with application startup despite seeding error. Please check the database configuration.");
            }

            // Use global exception handling middleware
            app.UseGlobalExceptionHandling();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseStaticFiles();

            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
