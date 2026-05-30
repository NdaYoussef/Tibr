using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tibr.Application.InfrastructureContracts;
using Tibr.Application.Services.SuppoertServices;
using Tibr.Application.Services.SupportServices;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;
using Tibr.Infrastructure.Queries;
using Tibr.Infrastructure.Repositories;

namespace Tibr.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            );

            // Mapster
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(Assembly.GetExecutingAssembly());
            services.AddSingleton(config);
            services.AddScoped<IMapper, Mapper>();

            // Generic repository
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

            // Query services
            services.AddScoped<IOrderQueryService, OrderQueryService>();

            // Support
            services.AddScoped<ISupportRepository, SupportRepository>();
            services.AddScoped<ISupportService, SupportService>();

            return services;
        }
    }
}
