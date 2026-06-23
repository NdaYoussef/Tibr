using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Services.MarketPriceService
{
    public interface IMarketPriceService
    {
        Task UpdateAssetPricesAsync();
    }
}
