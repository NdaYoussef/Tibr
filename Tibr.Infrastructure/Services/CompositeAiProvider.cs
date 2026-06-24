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
        private readonly HuggingFaceProviderService _huggingFace;
        private readonly AiChatSettings _settings;
        private readonly ILogger<CompositeAiProvider> _logger;

        public CompositeAiProvider(
            GeminiProviderService gemini,
            OpenAiProviderService openAi,
            XaiProviderService xai,
            HuggingFaceProviderService huggingFace,
            IOptions<AiChatSettings> settings,
            ILogger<CompositeAiProvider> logger)
        {
            _gemini = gemini;
            _openAi = openAi;
            _xai = xai;
            _huggingFace = huggingFace;
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
            var provider = _settings.EmbeddingProvider.ToLowerInvariant();
            _logger.LogDebug("CompositeAiProvider routing embedding to {Provider}", provider);

            return provider switch
            {
                "huggingface" => _huggingFace.EmbedBatchAsync(texts),
                "openai" => _openAi.EmbedBatchAsync(texts),
                _ => _gemini.EmbedBatchAsync(texts)
            };
        }

        public Task<float[]> EmbedAsync(string text)
        {
            var provider = _settings.EmbeddingProvider.ToLowerInvariant();

            return provider switch
            {
                "huggingface" => _huggingFace.EmbedAsync(text),
                "openai" => _openAi.EmbedAsync(text),
                _ => _gemini.EmbedAsync(text)
            };
        }
    }
}
