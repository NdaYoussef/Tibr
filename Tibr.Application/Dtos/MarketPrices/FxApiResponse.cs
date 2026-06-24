using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.MarketPrices
{
    public class FxApiResponse
    {
        public string Base { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }
}
