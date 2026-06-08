using System;
using System.Net.Http.Json;
using Tibr.Application.Dtos.MarketPrices;
using Tibr.Application.Services.MarketPriceService;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;

namespace Tibr.Infrastructure.Services
{
    public class MarketPriceService : IMarketPriceService
    {
        private readonly HttpClient _httpClient;
        private readonly IAssetPriceRepository _assetPriceRepository;

        private const decimal TroyOunceToGram = 31.1034768m;

        public MarketPriceService(
        HttpClient httpClient,
        IAssetPriceRepository assetPriceRepository)
        {
            _httpClient = httpClient;
            _assetPriceRepository = assetPriceRepository;
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
            }

            if (silverPriceUsd > 0)
            {
                var silverGramPrice =
                    (silverPriceUsd / TroyOunceToGram) * usdRate;

                await UpdateAssetAsync(
                    AssetType.Silver,
                    silverGramPrice,
                    "gold-api + fxapi");
            }

            await _assetPriceRepository.SaveChangesAsync();
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
    }
}
