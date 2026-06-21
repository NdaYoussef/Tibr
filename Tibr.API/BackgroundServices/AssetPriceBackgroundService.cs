using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tibr.Application.Services.MarketPriceService;

public class AssetPriceBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AssetPriceBackgroundService> _logger;

    public AssetPriceBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AssetPriceBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var service =
                    scope.ServiceProvider.GetRequiredService<IMarketPriceService>();

                await service.UpdateAssetPricesAsync();

                _logger.LogInformation(
                    "Asset prices updated at {Time}",
                    DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating asset prices");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(1),
                stoppingToken);
        }
    }
}