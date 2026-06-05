using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.AssetPriceServices
{
    public class AssetPriceService : IAssetPriceService
    {
        private readonly IGenericRepository<AssetPrice, long> _assetPriceRepo;

        public AssetPriceService(IGenericRepository<AssetPrice, long> assetPriceRepo)
        {
            _assetPriceRepo = assetPriceRepo;
        }

        public async Task<Result<AssetPriceDto?>> GetCurrentPriceAsync(AssetType assetType)
        {
            var price = _assetPriceRepo.GetAll(p => p.AssetType == assetType)
                .OrderByDescending(p => p.CreatedAt).FirstOrDefault();

            if (price is null)
                return Result<AssetPriceDto?>.Success(null);

            return Result<AssetPriceDto?>.Success(new AssetPriceDto
            {
                AssetType = price.AssetType,
                BuyPrice = price.BuyPrice,
                SellPrice = price.SellPrice,
                Source = price.Source,
                CreatedAt = price.CreatedAt
            });
        }

        public async Task<Result> RecordPriceAsync(AssetType assetType, decimal buyPrice, decimal sellPrice, string source)
        {
            var price = new AssetPrice
            {
                AssetType = assetType,
                BuyPrice = buyPrice,
                SellPrice = sellPrice,
                Source = source
            };

            await _assetPriceRepo.AddAsync(price);
            await _assetPriceRepo.SaveChangesAsync();

            return Result.Success();
        }
    }
}
