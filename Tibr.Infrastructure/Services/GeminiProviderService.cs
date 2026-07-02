using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tibr.Application.Services.AiChatServices;
using Tibr.Infrastructure.Config;

namespace Tibr.Infrastructure.Services
{
    public class GeminiProviderService : IAiProviderService
    {
        private readonly HttpClient _http;
        private readonly AiChatSettings _settings;
        private readonly ILogger<GeminiProviderService> _logger;

        public GeminiProviderService(
            HttpClient http,
            IOptions<AiChatSettings> settings,
            ILogger<GeminiProviderService> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<float[]> EmbedAsync(string text)
        {
            var results = await EmbedBatchAsync([text]);
            return results[0];
        }

        public async Task<List<float[]>> EmbedBatchAsync(List<string> texts)
        {
            var embedKey = _settings.GetEffectiveEmbeddingApiKey();
            var requests = texts.Select(t => new
            {
                model = $"models/{_settings.EmbeddingModel}",
                content = new { parts = new[] { new { text = t } } }
            }).ToList();

            var url = $"{_settings.BaseUrl}/models/{_settings.EmbeddingModel}"
                + $":batchEmbedContents?key={embedKey}";

            HttpResponseMessage? res = null;
            try
            {
                res = await _http.PostAsJsonAsync(url, new { requests });
                res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini EmbedBatchAsync failed ({StatusCode})",
                    res?.StatusCode.ToString() ?? "no response");
                return texts.Select(_ => Array.Empty<float>()).ToList();
            }

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();

            return json.GetProperty("embeddings")
                .EnumerateArray()
                .Select(e =>
                    e.GetProperty("values").EnumerateArray().Select(x => x.GetSingle()).ToArray())
                .ToList();
        }

        public async Task<AssistantResponse> ChatAsync(
            string systemPrompt,
            List<Message> history,
            List<object>? tools = null)
        {
            var contents = BuildContents(history);

            object body;
            if (tools is { Count: > 0 })
            {
                body = new
                {
                    system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents,
                    tools = new object[] { new { functionDeclarations = tools } }
                };
            }
            else
            {
                body = new
                {
                    system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents
                };
            }

            var url = $"{_settings.BaseUrl}/models/{_settings.ChatModel}"
                + $":generateContent?key={_settings.ChatApiKey}";

            HttpResponseMessage? res = null;
            string? errorBody = null;
            try
            {
                res = await _http.PostAsJsonAsync(url, body);
                res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                try { errorBody = res?.Content is not null ? await res.Content.ReadAsStringAsync() : null; } catch { }
                _logger.LogError(ex, "Gemini ChatAsync failed ({StatusCode}): {ErrorBody}",
                    res?.StatusCode.ToString() ?? "no response", errorBody ?? "(no body)");
                return new AssistantResponse(
                    "I'm sorry, the AI service is temporarily unavailable. Please try again shortly.", null);
            }

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var candidate = json.GetProperty("candidates")[0].GetProperty("content");
            var parts = candidate.GetProperty("parts").EnumerateArray().ToList();

            string? content = null;
            List<ToolCall>? calls = null;

            var textParts = parts.Where(p => p.TryGetProperty("text", out _)).ToList();
            if (textParts.Count > 0)
                content = string.Join("", textParts.Select(p => p.GetProperty("text").GetString()));

            var fcParts = parts.Where(p => p.TryGetProperty("functionCall", out _)).ToList();
            if (fcParts.Count > 0)
            {
                calls = fcParts.Select((p, i) =>
                {
                    var fc = p.GetProperty("functionCall");
                    return new ToolCall(
                        $"{i}",
                        fc.GetProperty("name").GetString()!,
                        fc.GetProperty("args").GetRawText());
                }).ToList();
            }

            return new AssistantResponse(content, calls);
        }

        private static List<object> BuildContents(List<Message> history)
        {
            var contents = new List<object>();

            foreach (var m in history)
            {
                var role = m.Role switch
                {
                    "user" => "user",
                    "model" => "model",
                    _ => "user"
                };

                contents.Add(new
                {
                    role,
                    parts = new[] { new { text = m.Content ?? "" } }
                });
            }

            return contents;
        }
    }
}
