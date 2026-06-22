using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tibr.Application.Services.AiChatServices;
using Tibr.Infrastructure.Config;

namespace Tibr.Infrastructure.Services
{
    public class CompositeAiProvider : IAiProviderService
    {
        private readonly GeminiProviderService _gemini;
        private readonly OpenAiProviderService _openAi;
        private readonly XaiProviderService _xai;
        private readonly AiChatSettings _settings;
        private readonly ILogger<CompositeAiProvider> _logger;

        public CompositeAiProvider(
            GeminiProviderService gemini,
            OpenAiProviderService openAi,
            XaiProviderService xai,
            IOptions<AiChatSettings> settings,
            ILogger<CompositeAiProvider> logger)
        {
            _gemini = gemini;
            _openAi = openAi;
            _xai = xai;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<AssistantResponse> ChatAsync(
            string systemPrompt,
            List<Message> history,
            List<object>? tools = null)
        {
            var provider = _settings.ChatProvider.ToLowerInvariant();
            _logger.LogDebug("CompositeAiProvider routing chat to {Provider}", provider);

            return provider switch
            {
                "openai" => _openAi.ChatAsync(systemPrompt, history, tools),
                "xai" => _xai.ChatAsync(systemPrompt, history, tools),
                _ => _gemini.ChatAsync(systemPrompt, history, tools)
            };
        }

        public Task<List<float[]>> EmbedBatchAsync(List<string> texts)
        {
            return _gemini.EmbedBatchAsync(texts);
        }

        public Task<float[]> EmbedAsync(string text)
        {
            return _gemini.EmbedAsync(text);
        }
    }
}
