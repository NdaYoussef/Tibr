using FluentValidation.AspNetCore;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Tibr.Application.Services.AddressServices;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.DeliveryServices;
using Tibr.Application.Services.DepositServices;
using Tibr.Application.Services.InvestmentOrderServices;
using Tibr.Application.Services.MarketPriceService;
using Tibr.Application.Services.OrderServices;
using Tibr.Application.Services.PaymentServices;
using Tibr.Application.Services.ResolutionServices;
using Tibr.Application.Services.ReviewServices;
using Tibr.Application.Services.TradeServices;
using Tibr.Application.Services.WalletServices;
using Tibr.Application.Services.WithdrawServices;
using Tibr.Application.Services.AiChatServices;

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
            services.AddScoped<IWithdrawService, WithdrawService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<PaymentService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IProposalResolutionClassifier, ProposalResolutionClassifier>();
            services.AddScoped<GoalParser>();
            services.AddMapster();
            services.AddFluentValidationAutoValidation()
                    .AddFluentValidationClientsideAdapters();


            return services;
        }
    }
}
