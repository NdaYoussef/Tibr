using Tibr.Application;
using Tibr.Application.Services.PaymentServices;
using Tibr.Infrastructure;
using Tibr.Infrastructure.Config;
using Tibr.Infrastructure.Services;

namespace Tibr.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            var configuration = builder.Configuration;

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

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
