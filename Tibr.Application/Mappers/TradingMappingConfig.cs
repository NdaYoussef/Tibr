using Mapster;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class TradingMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<OrdersInvestment, InvestmentOrderDto>()
                .Map(dest => dest.Conditions, src => src.Conditions)
                .Map(dest => dest.Trades, src => src.Trades);

            config.NewConfig<OrderCondition, OrderConditionDto>();

            config.NewConfig<Trade, TradeDto>();
        }
    }
}
