using System;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos.MarketPrices;
using Tibr.Application.Services.MarketPriceService;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Infrastructure.Services
{
    public class MarketPriceService : IMarketPriceService
    {
        private readonly HttpClient _httpClient;
        private readonly IAssetPriceRepository _assetPriceRepository;
        private readonly ApplicationDbContext _dbContext;

        private const decimal TroyOunceToGram = 31.1034768m;

        public MarketPriceService(
        HttpClient httpClient,
        IAssetPriceRepository assetPriceRepository,
        ApplicationDbContext dbContext)
        {
            _httpClient = httpClient;
            _assetPriceRepository = assetPriceRepository;
            _dbContext = dbContext;
        }

        public async Task UpdateAssetPricesAsync()
        {
            var usdRate = await GetUsdToEgpRateAsync();

            if (usdRate <= 0)
                return;

            var goldPriceUsd = await GetGoldPriceAsync();
            var silverPriceUsd = await GetSilverPriceAsync();

            if (goldPriceUsd > 0)
            {
                var goldGramPrice =
                    (goldPriceUsd / TroyOunceToGram) * usdRate;

                await UpdateAssetAsync(
                    AssetType.Gold,
                    goldGramPrice,
                    "gold-api + fxapi");

                await EnsureDailySnapshotAsync(AssetType.Gold, Math.Round(goldGramPrice, 2));
            }

            if (silverPriceUsd > 0)
            {
                var silverGramPrice =
                    (silverPriceUsd / TroyOunceToGram) * usdRate;

                await UpdateAssetAsync(
                    AssetType.Silver,
                    silverGramPrice,
                    "gold-api + fxapi");

                await EnsureDailySnapshotAsync(AssetType.Silver, Math.Round(silverGramPrice, 2));
            }

            await _assetPriceRepository.SaveChangesAsync();
            await _dbContext.SaveChangesAsync();
        }
        private async Task<decimal> GetGoldPriceAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<GoldApiResponse>(
                "https://api.gold-api.com/price/XAU/USD");

            return response?.Price ?? 0;
        }

        private async Task<decimal> GetSilverPriceAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<GoldApiResponse>(
                "https://api.gold-api.com/price/XAG/USD");

            return response?.Price ?? 0;
        }

        private async Task<decimal> GetUsdToEgpRateAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<FxApiResponse>(
                "https://fxapi.app/api/USD/EGP.json");

            return response?.Rate ?? 0;
        }

        private async Task UpdateAssetAsync(
    AssetType assetType,
    decimal gramPrice,
    string source)
        {
            var asset = _assetPriceRepository
                .GetAll(x => x.AssetType == assetType)
                .FirstOrDefault();

            if (asset == null)
            {
                asset = new AssetPrice
                {
                    AssetType = assetType,
                    BuyPrice = Math.Round(gramPrice, 2),
                    SellPrice = Math.Round(gramPrice * 1.05m, 2),
                    Source = source
                };

                await _assetPriceRepository.AddAsync(asset);
            }
            else
            {
                asset.BuyPrice = Math.Round(gramPrice, 2);
                asset.SellPrice = Math.Round(gramPrice * 1.05m, 2);
                asset.Source = source;
            }
        }

        private async Task EnsureDailySnapshotAsync(AssetType assetType, decimal price)
        {
            var today = DateTime.UtcNow.Date;
            var existing = await _dbContext.PriceSnapshots
                .FirstOrDefaultAsync(ps => ps.AssetType == assetType && ps.SnapshotDate == today);

            if (existing != null)
                return;

            var snapshot = new PriceSnapshot
            {
                AssetType = assetType,
                Price = price,
                SnapshotDate = today
            };

            try
            {
                _dbContext.PriceSnapshots.Add(snapshot);
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _dbContext.Entry(snapshot).State = EntityState.Detached;
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException?.Message?.Contains("UNIQUE") == true
            || ex.InnerException?.Message?.Contains("unique") == true;
    }
}
