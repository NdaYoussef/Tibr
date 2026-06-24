using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.TradeServices
{
    public interface ITradeService
    {
        Task<Result<InvestmentOrderDto>> ExecuteDirectBuyAsync(long userId, DirectBuyDto dto);
        Task<Result<InvestmentOrderDto>> ExecuteDirectSellAsync(long userId, DirectSellDto dto);
    }
}
