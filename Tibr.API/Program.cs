using Microsoft.EntityFrameworkCore;
using Tibr.Application;
using Tibr.Application.Services;
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

            // Add services to the container.

            builder.Services.AddControllers();

            var configuration = builder.Configuration;
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            );

            builder.Services.AddInfrastructureServices();
            builder.Services.AddApplicationServices();

            builder.Services.Configure<PaymobSettings>(
                builder.Configuration.GetSection(PaymobSettings.SectionName)
            );
            builder.Services.AddHttpClient<IPaymobService, PaymobService>();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
