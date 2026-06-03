using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos
{
    public class AddToCartDto
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
