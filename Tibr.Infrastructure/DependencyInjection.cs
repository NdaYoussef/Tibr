using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tibr.Application.Services.CategoryServices;
using Tibr.Application.Services.ProductServices;
using Tibr.Application.Services.SuppoertServices;
using Tibr.Application.Services.SupportServices;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;
using Tibr.Infrastructure.Repositories;

namespace Tibr.Infrastructure
{
    public static class DependecyInjection
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

            //repos register
            services.AddScoped<ISupportRepository, SupportRepository>();
            services.AddScoped<ISupportService, SupportService>();

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductService, ProductService>();

            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryService, CategoryService>();

            return services;

        }
    }
}
