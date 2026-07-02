using Tibr.Application.Dtos;
using Tibr.Domain.Enums;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.AssetPriceServices
{
    public interface IAssetPriceService
    {
        Task<Result<AssetPriceDto?>> GetCurrentPriceAsync(AssetType assetType);
        Task<Result> RecordPriceAsync(AssetType assetType, decimal buyPrice, decimal sellPrice, string source);
        Task<Result<PriceAnalyticsDto>> GetPriceAnalyticsAsync(AssetType assetType);
    }
}
