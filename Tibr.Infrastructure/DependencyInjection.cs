using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tibr.Application.InfrastructureContracts;
using Tibr.Application.Services.AiChatServices;
using Tibr.Application.Services.CartServices;
using Tibr.Application.Services.CategoryServices;
using Tibr.Application.Services.FavoriteServices;
using Tibr.Application.Services.ProductServices;
using Tibr.Application.Services.SuppoertServices;
using Tibr.Application.Services.SupportServices;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Config;
using Tibr.Infrastructure.Contexts;
using Tibr.Infrastructure.Queries;
using Tibr.Infrastructure.Repositories;
using Tibr.Infrastructure.Services;
using Tibr.Application.Interfaces;
using Tibr.Application.Services.NotificationServices;
using Tibr.Infrastructure.Services.NotificationServices;

namespace Tibr.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            );

            services.AddScoped<DbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>()
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

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductService, ProductService>();

            services.AddScoped<IFavoriteRepository, FavoriteRepository>();
            services.AddScoped<IFavoriteService, FavoriteService>();

            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryService, CategoryService>();

            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<ICartService, CartService>();

            services.AddScoped<IAssetPriceRepository, AssetPriceRepository>();

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<ITicketRepository, TicketRepository>();


            services.AddSingleton<IVectorStoreService, InMemoryVectorStoreService>();

            var vectorStoreType = configuration.GetValue<string>("VectorStore");

            if (vectorStoreType == "Qdrant")
            {
                services.Configure<QdrantSettings>(
                    configuration.GetSection(QdrantSettings.SectionName));

                services.AddHttpClient<IVectorStoreService, QdrantVectorStoreService>();
            }
            else
            {
                services.AddSingleton<IVectorStoreService, InMemoryVectorStoreService>();
            }

            services.AddHostedService<ChatSeedHostedService>();

            services.AddHttpClient<GeminiProviderService>();
            services.AddHttpClient<OpenAiProviderService>();
            services.AddHttpClient<XaiProviderService>();
            services.AddHttpClient<HuggingFaceProviderService>();
            services.AddSingleton<IAiProviderService, CompositeAiProvider>();

            services.AddScoped<IChatOrderProposalService, ChatOrderProposalService>();

            services.Configure<AiChatSettings>(
                configuration.GetSection(AiChatSettings.SectionName)
            );
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAdminNotificationPublisher, SignalRNotificationPublisher>();
            services.AddSignalR();

            return services;
        }
    }
}