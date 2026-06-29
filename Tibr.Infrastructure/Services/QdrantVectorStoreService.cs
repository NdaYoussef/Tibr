using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tibr.Application.Services.AiChatServices;
using Tibr.Infrastructure.Config;

namespace Tibr.Infrastructure.Services;

public class QdrantVectorStoreService : IVectorStoreService
{
    private const string FaqCollection = "faq";
    private const string FactsCollection = "facts";

    private const float DirectHitMinScore = 0.85f;
    private const float DirectHitMinGap = 0.15f;

    private readonly HttpClient _http;
    private readonly IAiProviderService _aiProvider;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantVectorStoreService> _logger;
    private readonly ConcurrentDictionary<string, bool> _collectionsCreated = new();
    private readonly ConcurrentDictionary<ulong, string> _faqIdLookup = new();
    private readonly ConcurrentDictionary<ulong, string> _factIdLookup = new();

    public QdrantVectorStoreService(
        HttpClient http,
        IAiProviderService aiProvider,
        IOptions<QdrantSettings> settings,
        ILogger<QdrantVectorStoreService> logger)
    {
        _http = http;
        _aiProvider = aiProvider;
        _settings = settings.Value;
        _logger = logger;
    }

        public async Task IndexFaqAsync(List<FaqEntry> entries)
        {
            await EnsureCollectionAsync(FaqCollection);

            var texts = entries.Select(e => e.QuestionAr is not null
                ? $"Q: {e.Question}\nA: {e.Answer}\nس: {e.QuestionAr}\nج: {e.AnswerAr ?? e.Answer}"
                : $"Q: {e.Question}\nA: {e.Answer}").ToList();
        var vectors = await _aiProvider.EmbedBatchAsync(texts);

        _faqIdLookup.Clear();

        var points = new List<object>();
        for (int i = 0; i < entries.Count; i++)
        {
            var ulid = ToQdrantId(entries[i].Id);
            _faqIdLookup[ulid] = entries[i].Id;

            points.Add(new
            {
                id = ulid,
                vector = vectors[i],
                payload = new
                {
                    id = entries[i].Id,
                    question = entries[i].Question,
                    question_ar = entries[i].QuestionAr ?? entries[i].Question,
                    answer = entries[i].Answer,
                    answer_ar = entries[i].AnswerAr ?? entries[i].Answer,
                },
            });
        }

        var body = new { points };
        var res = await _http.PutAsJsonAsync(
            $"{_settings.Url}/collections/{FaqCollection}/points", body);
        res.EnsureSuccessStatusCode();

        _logger.LogInformation("Indexed {Count} FAQ entries into Qdrant", entries.Count);
    }

    public async Task IndexFactsAsync(List<FactEntry> entries)
    {
        await EnsureCollectionAsync(FactsCollection);

        var texts = entries.Select(e => e.Content).ToList();
        var vectors = await _aiProvider.EmbedBatchAsync(texts);

        _factIdLookup.Clear();

        var points = new List<object>();
        for (int i = 0; i < entries.Count; i++)
        {
            var ulid = ToQdrantId(entries[i].Id);
            _factIdLookup[ulid] = entries[i].Id;

            points.Add(new
            {
                id = ulid,
                vector = vectors[i],
                payload = new
                {
                    id = entries[i].Id,
                    content = entries[i].Content,
                },
            });
        }

        var body = new { points };
        var res = await _http.PutAsJsonAsync(
            $"{_settings.Url}/collections/{FactsCollection}/points", body);
        res.EnsureSuccessStatusCode();

        _logger.LogInformation("Indexed {Count} fact entries into Qdrant", entries.Count);
    }

    public async Task<FaqRetrievalResult> SearchFaqAsync(
        string query,
        int topK = 3,
        float minScore = 0.5f)
    {
        var queryVec = await _aiProvider.EmbedAsync(query);

        QdrantSearchResponse? response;
        try
        {
            var body = new
            {
                vector = queryVec,
                limit = topK,
                score_threshold = minScore,
                with_payload = true,
            };
            var res = await _http.PostAsJsonAsync(
                $"{_settings.Url}/collections/{FaqCollection}/points/search", body);
            res.EnsureSuccessStatusCode();
            response = await res.Content.ReadFromJsonAsync<QdrantSearchResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant FAQ search failed");
            return new FaqRetrievalResult([], false);
        }

        if (response?.Result is null || response.Result.Count == 0)
            return new FaqRetrievalResult([], false);

        var hits = response.Result.Select(r =>
        {
            var id = r.Payload?.GetProperty("id").GetString() ?? "unknown";
            var question = r.Payload?.GetProperty("question").GetString() ?? "";
            var questionAr = r.Payload?.TryGetProperty("question_ar", out var qa) == true ? qa.GetString() : null;
            var answer = r.Payload?.GetProperty("answer").GetString() ?? "";
            var answerAr = r.Payload?.TryGetProperty("answer_ar", out var aa) == true ? aa.GetString() : null;
            var entry = new FaqEntry(id, question, answer, answerAr, questionAr);
            return new ScoredFaq(entry, (float)r.Score);
        }).ToList();

        bool isDirect = hits.Count >= 1
            && hits[0].Score >= DirectHitMinScore
            && (hits.Count == 1 || hits[0].Score - hits[1].Score >= DirectHitMinGap);

        return new FaqRetrievalResult(hits, isDirect);
    }

    public async Task<List<ScoredFact>> SearchFactsAsync(
        string query,
        int topK = 3,
        float minScore = 0.4f)
    {
        var queryVec = await _aiProvider.EmbedAsync(query);

        QdrantSearchResponse? response;
        try
        {
            var body = new
            {
                vector = queryVec,
                limit = topK,
                score_threshold = minScore,
                with_payload = true,
            };
            var res = await _http.PostAsJsonAsync(
                $"{_settings.Url}/collections/{FactsCollection}/points/search", body);
            res.EnsureSuccessStatusCode();
            response = await res.Content.ReadFromJsonAsync<QdrantSearchResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant facts search failed");
            return [];
        }

        if (response?.Result is null)
            return [];

        return response.Result.Select(r =>
        {
            var id = r.Payload?.GetProperty("id").GetString() ?? "unknown";
            var content = r.Payload?.GetProperty("content").GetString() ?? "";
            return new ScoredFact(new FactEntry(id, content), (float)r.Score);
        }).ToList();
    }

    private async Task EnsureCollectionAsync(string collectionName)
    {
        if (_collectionsCreated.ContainsKey(collectionName))
            return;

        try
        {
            var existing = await _http.GetAsync(
                $"{_settings.Url}/collections/{collectionName}");

            if (!existing.IsSuccessStatusCode)
            {
                var createBody = new
                {
                    vectors = new
                    {
                        size = _settings.VectorSize,
                        distance = "Cosine",
                    },
                };
                var res = await _http.PutAsJsonAsync(
                    $"{_settings.Url}/collections/{collectionName}", createBody);
                res.EnsureSuccessStatusCode();
                _logger.LogInformation("Created Qdrant collection {Collection}", collectionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Qdrant not reachable for collection {Collection}. " +
                "Vector search will return empty results.", collectionName);
            return;
        }

        _collectionsCreated[collectionName] = true;
    }

    private static ulong ToQdrantId(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return BitConverter.ToUInt64(hash, 0);
    }
}

public class QdrantSearchResponse
{
    [JsonPropertyName("result")]
    public List<QdrantScoredPoint>? Result { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class QdrantScoredPoint
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; set; }

    [JsonPropertyName("version")]
    public long Version { get; set; }
}
