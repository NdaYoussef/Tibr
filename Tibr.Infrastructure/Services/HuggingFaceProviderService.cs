using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tibr.Application.Services.AiChatServices;
using Tibr.Infrastructure.Config;

namespace Tibr.Infrastructure.Services
{
    public class HuggingFaceProviderService : IAiProviderService
    {
        private readonly HttpClient _http;
        private readonly AiChatSettings _settings;
        private readonly ILogger<HuggingFaceProviderService> _logger;

        public HuggingFaceProviderService(
            HttpClient http,
            IOptions<AiChatSettings> settings,
            ILogger<HuggingFaceProviderService> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<AssistantResponse> ChatAsync(
            string systemPrompt,
            List<Message> history,
            List<object>? tools = null)
        {
            throw new NotSupportedException("HuggingFace provider does not support chat. Use Gemini, OpenAI, or xAI.");
        }

        public async Task<float[]> EmbedAsync(string text)
        {
            var results = await EmbedBatchAsync([text]);
            return results[0];
        }

        public async Task<List<float[]>> EmbedBatchAsync(List<string> texts)
        {
            var url = string.IsNullOrEmpty(_settings.EmbeddingBaseUrl)
                ? "https://router.huggingface.co/hf-inference/models/BAAI/bge-large-en-v1.5/pipeline/feature-extraction"
                : _settings.EmbeddingBaseUrl;

            object body = texts.Count == 1
                ? new { inputs = texts[0] }
                : new { inputs = texts };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.EmbeddingApiKey);
            request.Content = JsonContent.Create(body);

            HttpResponseMessage? res = null;
            try
            {
                res = await _http.SendAsync(request);
                res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HuggingFace EmbedBatchAsync failed ({StatusCode})",
                    res?.StatusCode.ToString() ?? "no response");
                return texts.Select(_ => Array.Empty<float>()).ToList();
            }

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            return ParseEmbeddingResponse(json);
        }

        private static List<float[]> ParseEmbeddingResponse(JsonElement json)
        {
            if (json.ValueKind != JsonValueKind.Array || json.GetArrayLength() == 0)
                return [];

            var first = json[0];

            // Flat float array → single embedding
            if (first.ValueKind == JsonValueKind.Number)
                return [json.EnumerateArray().Select(x => x.GetSingle()).ToArray()];

            // 2D array (array of flat float arrays) → batch of embeddings
            if (first.ValueKind == JsonValueKind.Array)
            {
                return json.EnumerateArray()
                    .Select(emb => emb.EnumerateArray().Select(x => x.GetSingle()).ToArray())
                    .ToList();
            }

            return [];
        }
    }
}
