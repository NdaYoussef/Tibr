using FluentValidation.AspNetCore;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Tibr.Application.Services.AddressServices;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.DeliveryServices;
using Tibr.Application.Services.DepositServices;
using Tibr.Application.Services.InvestmentOrderServices;
using Tibr.Application.Services.OrderServices;
using Tibr.Application.Services.ResolutionServices;
using Tibr.Application.Services.TradeServices;
using Tibr.Application.Services.WalletServices;

namespace Tibr.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<IAssetPriceService, AssetPriceService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<ITradeService, TradeService>();
            services.AddScoped<IInvestmentOrderService, InvestmentOrderService>();
            services.AddScoped<IResolutionService, ResolutionService>();
            services.AddScoped<IDepositService, DepositService>();
            services.AddScoped<IDeliveryService, DeliveryService>();
            services.AddMapster();
            services.AddFluentValidationAutoValidation()
                    .AddFluentValidationClientsideAdapters();
            return services;
        }
    }
}
