using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tibr.Application.Services.AiChatServices;
using Tibr.Infrastructure.Config;

namespace Tibr.Infrastructure.Services
{
    public class OpenAiProviderService : IAiProviderService
    {
        private readonly HttpClient _http;
        private readonly AiChatSettings _settings;
        private readonly ILogger<OpenAiProviderService> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public OpenAiProviderService(
            HttpClient http,
            IOptions<AiChatSettings> settings,
            ILogger<OpenAiProviderService> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<AssistantResponse> ChatAsync(
            string systemPrompt,
            List<Message> history,
            List<object>? tools = null)
        {
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            foreach (var m in history)
            {
                messages.Add(new
                {
                    role = m.Role == "model" ? "assistant" : m.Role,
                    content = m.Content ?? ""
                });
            }

            object body;
            if (tools is { Count: > 0 })
            {
                var openAiTools = tools.Select(t => new
                {
                    type = "function",
                    function = t
                }).ToList();

                body = new
                {
                    model = _settings.ChatModel,
                    messages,
                    tools = openAiTools
                };
            }
            else
            {
                body = new
                {
                    model = _settings.ChatModel,
                    messages
                };
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ChatApiKey);
            request.Content = JsonContent.Create(body, options: JsonOpts);

            HttpResponseMessage? res = null;
            try
            {
                res = await _http.SendAsync(request);
                res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI ChatAsync failed ({StatusCode})",
                    res?.StatusCode.ToString() ?? "no response");
                return new AssistantResponse(
                    "I'm sorry, the AI service is temporarily unavailable. Please try again shortly.", null);
            }

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var choice = json.GetProperty("choices")[0].GetProperty("message");

            string? content = null;
            List<ToolCall>? calls = null;

            if (choice.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                content = contentEl.GetString();

            if (choice.TryGetProperty("tool_calls", out var toolCallsEl) && toolCallsEl.ValueKind == JsonValueKind.Array)
            {
                calls = toolCallsEl.EnumerateArray().Select((tc, i) =>
                {
                    var fn = tc.GetProperty("function");
                    return new ToolCall(
                        tc.TryGetProperty("id", out var id) ? id.GetString()! : $"call_{i}",
                        fn.GetProperty("name").GetString()!,
                        fn.GetProperty("arguments").GetString()!
                    );
                }).ToList();
            }

            return new AssistantResponse(content, calls);
        }

        public async Task<List<float[]>> EmbedBatchAsync(List<string> texts)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ChatApiKey);

            var body = new
            {
                model = "text-embedding-3-small",
                input = texts
            };
            request.Content = JsonContent.Create(body, options: JsonOpts);

            HttpResponseMessage? res = null;
            try
            {
                res = await _http.SendAsync(request);
                res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI EmbedBatchAsync failed ({StatusCode})",
                    res?.StatusCode.ToString() ?? "no response");
                return texts.Select(_ => Array.Empty<float>()).ToList();
            }

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("data")
                .EnumerateArray()
                .OrderBy(e => e.GetProperty("index").GetInt32())
                .Select(e =>
                    e.GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray())
                .ToList();
        }

        public async Task<float[]> EmbedAsync(string text)
        {
            var results = await EmbedBatchAsync([text]);
            return results[0];
        }
    }
}
