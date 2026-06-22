using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tibr.Application.Services.AiChatServices;

namespace Tibr.Infrastructure.Services
{
    public class ChatSeedHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatSeedHostedService> _logger;

        public ChatSeedHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<ChatSeedHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStoreService>();

                _logger.LogInformation("Seeding FAQ entries into vector store...");
                await vectorStore.IndexFaqAsync(KnowledgeBaseSeeder.FaqEntries());

                _logger.LogInformation("Seeding fact entries into vector store...");
                await vectorStore.IndexFactsAsync(KnowledgeBaseSeeder.FactEntries());

                _logger.LogInformation("Vector store seeded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed vector store on startup.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
