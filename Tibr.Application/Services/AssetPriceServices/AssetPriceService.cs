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
        private readonly IGenericRepository<PriceSnapshot, long> _snapshotRepo;

        public AssetPriceService(
            IGenericRepository<AssetPrice, long> assetPriceRepo,
            IGenericRepository<PriceSnapshot, long> snapshotRepo)
        {
            _assetPriceRepo = assetPriceRepo;
            _snapshotRepo = snapshotRepo;
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
                Source = source,
                CreatedAt = DateTime.UtcNow,
            };

            await _assetPriceRepo.AddAsync(price);
            await _assetPriceRepo.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<PriceAnalyticsDto>> GetPriceAnalyticsAsync(AssetType assetType)
        {
            var currentPrice = _assetPriceRepo.GetAll(p => p.AssetType == assetType)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => p.BuyPrice)
                .FirstOrDefault();

            var utcNow = DateTime.UtcNow;
            var thirtyDaysAgo = utcNow.AddDays(-30);

            var snapshots = _snapshotRepo
                .GetAll(ps => ps.AssetType == assetType && ps.SnapshotDate >= thirtyDaysAgo && !ps.IsDeleted)
                .OrderByDescending(ps => ps.SnapshotDate)
                .ToList();

            var daysOfData = snapshots.Count;

            var avgPrice = daysOfData > 0
                ? Math.Round(snapshots.Average(ps => ps.Price), 2)
                : 0m;

            var maxPrice = daysOfData > 0 ? snapshots.Max(ps => ps.Price) : 0m;
            var minPrice = daysOfData > 0 ? snapshots.Min(ps => ps.Price) : 0m;

            var isBelowAverage = avgPrice > 0 && currentPrice < avgPrice;
            decimal? percentBelowAverage = avgPrice > 0
                ? Math.Round((currentPrice - avgPrice) / avgPrice * 100, 2)
                : null;

            var range = maxPrice - minPrice;
            var isNearMonthlyLow = range > 0 && daysOfData >= 7
                && (currentPrice - minPrice) / range < 0.1m;

            return Result<PriceAnalyticsDto>.Success(new PriceAnalyticsDto
            {
                AssetType = assetType,
                CurrentPrice = currentPrice,
                AvgPriceLast30Days = avgPrice,
                MinPriceLast30Days = minPrice,
                MaxPriceLast30Days = maxPrice,
                DaysOfData = daysOfData,
                IsBelowAverage = isBelowAverage,
                PercentBelowAverage = percentBelowAverage,
                IsNearMonthlyLow = isNearMonthlyLow
            });
        }
    }
}
