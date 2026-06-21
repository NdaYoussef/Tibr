using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tibr.Application.InfrastructureContracts;
using Tibr.Application.Services.AdminServices;
using Tibr.Application.Services.CartServices;
using Tibr.Application.Services.CategoryServices;
using Tibr.Application.Services.FavoriteServices;
using Tibr.Application.Services.ProductServices;
using Tibr.Application.Services.SuppoertServices;
using Tibr.Application.Services.SupportServices;

using Tibr.Application.Services.TicketServices;

using Tibr.Application.Services.UserServices;

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

            services.AddMemoryCache();

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

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductService, ProductService>();

            services.AddScoped<IFavoriteRepository, FavoriteRepository>();
            services.AddScoped<IFavoriteService, FavoriteService>();

            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryService, CategoryService>();

            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<ICartService, CartService>();

<<<<<<< HEAD

            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<ITicketService, TicketService>();

            services.AddScoped<IAdminService, AdminService>();

            services.AddScoped<IAnalyticsService, AnalyticsService>();


            // User
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();

=======
            services.AddScoped<IAssetPriceRepository, AssetPriceRepository>();
>>>>>>> investment-dev-back

            return services;
        }
    }
}

