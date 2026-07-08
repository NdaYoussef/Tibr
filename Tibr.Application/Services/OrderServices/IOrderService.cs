using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.OrderServices
{
    public interface IOrderService
    {
        Task<Result<OrderDto>> GetByIdAsync(long id);
        Task<Result<IEnumerable<OrderDto>>> GetAllAsync();
        Task<Result<IEnumerable<OrderDto>>> GetByUserIdAsync(long userId);
        Task<Result<OrderDto>> CreateAsync(CreateOrderDto createDto);
        Task<Result<OrderDto>> CreateFromWalletAsync(long userId, WalletCheckoutDto dto);
        Task<Result<OrderDto>> PayWithWalletAsync(long userId, long orderId);
        Task<Result<OrderDto>> UpdateAsync(long id, UpdateOrderDto updateDto);
        Task<Result> DeleteAsync(long id);
    }
}
