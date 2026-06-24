using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.InvestmentOrderServices
{
    public interface IInvestmentOrderService
    {
        Task<Result<InvestmentOrderDto>> CreateStrategyOrderAsync(long userId, CreateStrategyOrderDto dto);
        Task<Result> CancelOrderAsync(long userId, long orderId);
        Task<Result<List<InvestmentOrderDto>>> GetUserOrdersAsync(long userId);
        Task<Result<InvestmentOrderDto>> GetOrderByIdAsync(long userId, long orderId);
    }
}
