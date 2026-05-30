using FluentValidation.AspNetCore;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Tibr.Application.Services.OrderServices;

namespace Tibr.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IOrderService, OrderService>();
            services.AddMapster();
            services.AddFluentValidationAutoValidation()
                    .AddFluentValidationClientsideAdapters();
            return services;
        }
    }
}
