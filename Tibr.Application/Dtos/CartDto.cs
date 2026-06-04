using System.Collections.Generic;
using System.Linq;

namespace Tibr.Application.Dtos
{
    public class CartDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public List<CartItemDto> CartItems { get; set; } = [];
        public decimal TotalAmount => CartItems.Sum(item => item.TotalPrice);

    }
}
