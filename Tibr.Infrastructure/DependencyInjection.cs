using Microsoft.Extensions.DependencyInjection;
using Tibr.Application.InfrastructureContracts;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Queries;
using Tibr.Infrastructure.Repositories;

namespace Tibr.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IOrderQueryService, OrderQueryService>();
            return services;
        }
    }
}
