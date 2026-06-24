using Tibr.Application.Services.ResolutionServices;

namespace Tibr.API.BackgroundServices
{
    public class ResolutionBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ResolutionBackgroundService> _logger;

        public ResolutionBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ResolutionBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IResolutionService>();
                    await service.EvaluateAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Resolution evaluation failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
