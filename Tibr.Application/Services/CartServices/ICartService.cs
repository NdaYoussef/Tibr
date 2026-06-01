using System.Threading.Tasks;
using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.CartServices
{
    public interface ICartService
    {
        Task<Result<CartDto>> GetCartByUserIdAsync(long userId);
        Task<Result<CartDto>> AddToCartAsync(long userId, AddToCartDto dto);
        Task<Result<CartDto>> RemoveFromCartAsync(long userId, long cartItemId);
        Task<Result<bool>> ClearCartAsync(long userId);
    }
}
