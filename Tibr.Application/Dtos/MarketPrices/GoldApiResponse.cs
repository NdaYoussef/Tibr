using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.MarketPrices
{
    public class GoldApiResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
