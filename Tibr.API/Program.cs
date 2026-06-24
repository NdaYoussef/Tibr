using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tibr.Application;
using Tibr.Application.Interfaces;
using Tibr.Application.Services.Email;
using Tibr.Application.Services.MarketPriceService;
using Tibr.Application.Services.PaymentServices;
using Tibr.Infrastructure;
using Tibr.Infrastructure.Config;
using Tibr.Infrastructure.Contexts;
using Tibr.Infrastructure.Services;

namespace Tibr.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ================= Logging =================
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // ================= Controllers =================
            builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                    opts.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter()));

            // ================= CORS =================
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // ================= Database =================
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<DbContext>(p =>
                p.GetRequiredService<ApplicationDbContext>());

            // ================= Services =================
            builder.Services.AddTransient<IEmailService, EmailService>();
            builder.Services.AddHttpClient<IPaymentGateway, PaymobPaymentGateway>();
            builder.Services.AddHttpClient<IMarketPriceService, MarketPriceService>();
            builder.Services.AddHostedService<AssetPriceBackgroundService>();

            // ================= JWT =================
            var configuration = builder.Configuration;

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
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!)),
                    ValidateLifetime = true
                };
            });

            // ================= MediatR =================

            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(
                    typeof(Tibr.Application.Services.Auth.RegisterCommand).Assembly);

                cfg.RegisterServicesFromAssembly(
                    typeof(Tibr.Infrastructure.DependencyInjection).Assembly);
            });

            builder.Services.AddOpenApi();

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplicationServices();

            // ================= Paymob =================
            builder.Services.Configure<PaymobSettings>(
                configuration.GetSection(PaymobSettings.SectionName));

            var app = builder.Build();

            // ================= DEV ONLY =================
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // ================= MIDDLEWARE ORDER =================

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors("AllowAll");

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}