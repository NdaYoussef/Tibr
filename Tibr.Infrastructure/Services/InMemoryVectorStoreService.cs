using Tibr.Application.Services.AiChatServices;

namespace Tibr.Infrastructure.Services
{
    public class InMemoryVectorStoreService : IVectorStoreService
    {
        private readonly IAiProviderService _aiProvider;
        private readonly List<FaqVector> _faqStore = [];
        private readonly List<FactVector> _factStore = [];

        public InMemoryVectorStoreService(IAiProviderService aiProvider)
        {
            _aiProvider = aiProvider;
        }

        public async Task IndexFaqAsync(List<FaqEntry> entries)
        {
            var texts = entries.Select(e => $"Q: {e.Question}\nA: {e.Answer}").ToList();
            var vectors = await _aiProvider.EmbedBatchAsync(texts);
            for (int i = 0; i < entries.Count; i++)
                _faqStore.Add(new FaqVector(entries[i], vectors[i]));
        }

        public async Task IndexFactsAsync(List<FactEntry> entries)
        {
            var texts = entries.Select(e => e.Content).ToList();
            var vectors = await _aiProvider.EmbedBatchAsync(texts);
            for (int i = 0; i < entries.Count; i++)
                _factStore.Add(new FactVector(entries[i], vectors[i]));
        }

        public async Task<FaqRetrievalResult> SearchFaqAsync(
            string query,
            int topK = 3,
            float minScore = 0.5f)
        {
            var queryVec = await _aiProvider.EmbedAsync(query);

            var hits = _faqStore
                .Select(f => (f, score: CosineSimilarity(queryVec, f.Vector)))
                .Where(x => x.score > minScore)
                .OrderByDescending(x => x.score)
                .Take(topK)
                .ToList();

            if (hits.Count == 0)
                return new FaqRetrievalResult([], false);

            const float directHitMin = 0.85f;
            const float minGap = 0.15f;

            bool isDirect =
                hits.Count >= 1
                && hits[0].score >= directHitMin
                && (hits.Count == 1 || hits[0].score - hits[1].score >= minGap);

            var results = hits.Select(h => new ScoredFaq(h.f.Entry, h.score)).ToList();
            return new FaqRetrievalResult(results, isDirect);
        }

        public async Task<List<FactEntry>> SearchFactsAsync(string query, float minScore = 0.4f)
        {
            var queryVec = await _aiProvider.EmbedAsync(query);

            return _factStore
                .Select(f => (f, score: CosineSimilarity(queryVec, f.Vector)))
                .Where(x => x.score > minScore)
                .OrderByDescending(x => x.score)
                .Select(x => x.f.Entry)
                .ToList();
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB) + 1e-10f);
        }

        private record FaqVector(FaqEntry Entry, float[] Vector);
        private record FactVector(FactEntry Entry, float[] Vector);
    }
}
