using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Tibr.Application.Interfaces;
using Tibr.Application.Services.Email;
using Tibr.Application.Mappers;
using Tibr.Infrastructure.Contexts;
using Tibr.Application;
using Tibr.Application.Services.PaymentServices;
using Tibr.Infrastructure;
using Tibr.Infrastructure.Config;
using Tibr.Infrastructure.Services;
using Tibr.Application.Services.CartServices;

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

            builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(CartProfile).Assembly));
            builder.Services.AddScoped<ICartService, CartService>();
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
            }).AddJwtBearer(options =>
            {
                var jwtSecret = configuration["JWT:Secret"];
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    throw new InvalidOperationException("JWT Secret Key is missing from appsettings.json!");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = configuration["JWT:ValidAudience"],
                    ValidIssuer = configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
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

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}