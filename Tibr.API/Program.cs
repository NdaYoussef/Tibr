using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tibr.API.BackgroundServices;
using Tibr.Application;
using Tibr.Application.Interfaces;
using Tibr.Application.Services.AiChatServices;
using Tibr.Application.Services.Email;
using Tibr.Application.Services.MarketPriceService;
using Tibr.Application.Services.PaymentServices;
using Tibr.Infrastructure;
using Tibr.Infrastructure.Config;
using Tibr.Infrastructure.Contexts;
using Tibr.Infrastructure.Seed;
using Tibr.Infrastructure.Services;
using Tibr.Infrastructure.Services.NotificationServices;
namespace Tibr.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure file logging
            var logsPath = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(logsPath);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddProvider(new FileLoggerProvider(logsPath));

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowAll",
                    policy =>
                    {
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    }
                );
            });
            builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                    opts.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter()));

            var configuration = builder.Configuration;

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            );

            builder.Services.AddScoped<DbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>()
            );
            builder.Services.AddTransient<IEmailService, EmailService>();
            builder
                .Services.AddAuthentication(options =>
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
                            Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!)
                        ),
                        ValidateLifetime = true,
                    };
                });

            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    typeof(Tibr.Application.Services.Auth.RegisterCommand).Assembly
                )
            );

            builder.Services.AddOpenApi();

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplicationServices();

            builder.Services.AddMemoryCache();

            builder.Services.Configure<PaymobSettings>(
                configuration.GetSection(PaymobSettings.SectionName)
            );

            builder.Services.Configure<PricingSettings>(
                configuration.GetSection(PricingSettings.SectionName)
            );

            var routingOptions = configuration
                .GetSection(ChatRoutingOptions.SectionName)
                .Get<ChatRoutingOptions>() ?? new ChatRoutingOptions();
            builder.Services.AddSingleton(routingOptions);
            builder.Services.AddHttpClient<IPaymentGateway, PaymobPaymentGateway>();

            builder.Services.AddHttpClient<IMarketPriceService, MarketPriceService>();
            builder.Services.AddHostedService<AssetPriceBackgroundService>();
            builder.Services.AddHostedService<ResolutionBackgroundService>();

            var app = builder.Build();

            // Apply migrations and seed if database is fresh
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await context.Database.MigrateAsync();

                if (!await context.Users.AnyAsync())
                {
                    Console.WriteLine("Database is empty. Seeding...");
                    var seeder = new MassDataSeeder(context);
                    await seeder.SeedAllAsync(userCount: 500);
                    Console.WriteLine("Database seeding complete.");
                }
            }

            if (args.Contains("--clear"))
            {
                Console.WriteLine("Clearing all data...");
                using var clearScope = app.Services.CreateScope();
                var context = clearScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cleaner = new DatabaseCleaner(context);
                await cleaner.ClearAllAsync();
                Console.WriteLine("All data cleared.");
                return;
            }

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseStaticFiles();
            app.UseCors("AllowAll");

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }


            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationHub>("/hubs/notifications");
            app.Run();
        }
    }
}

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logsPath;

    public FileLoggerProvider(string logsPath)
    {
        _logsPath = logsPath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_logsPath, categoryName);
    }

    public void Dispose() { }
}

public class FileLogger : ILogger
{
    private readonly string _logsPath;
    private readonly string _categoryName;
    private readonly object _syncLock = new object();

    public FileLogger(string logsPath, string categoryName)
    {
        _logsPath = logsPath;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"{timestamp} [{logLevel:g}] {_categoryName}: {message}";

        if (exception != null)
            logEntry += $"\nException: {exception}";

        // Write to all-logs file
        WriteToFile("all-logs.txt", logEntry);

        // Write to payment-callback file if this is a payment-related log
        if (_categoryName.Contains("Payment"))
            WriteToFile("payment-callback.txt", logEntry);
    }

    private void WriteToFile(string fileName, string logEntry)
    {
        lock (_syncLock)
        {
            try
            {
                var filePath = Path.Combine(_logsPath, fileName);
                File.AppendAllText(filePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Fallback: write to console if file writing fails
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}