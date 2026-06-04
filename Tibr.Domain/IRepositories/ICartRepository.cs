using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tibr.Domain.Entities;

namespace Tibr.Domain.IRepositories
{
    public interface ICartRepository : IGenericRepository<Cart, long>
    {
        Task<Cart?> GetCartByUserIdAsync(long userId);
        void RemoveCartItem(CartItem cartItem);
        void RemoveCartItemsRange(IEnumerable<CartItem> cartItems);
    }
}
