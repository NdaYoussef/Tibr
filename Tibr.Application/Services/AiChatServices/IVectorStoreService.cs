namespace Tibr.Application.Services.AiChatServices
{
    public record FaqEntry(string Id, string Question, string Answer);

    public record FactEntry(string Id, string Content);

    public record ScoredFaq(FaqEntry Entry, float Score);

    public record FaqRetrievalResult(List<ScoredFaq> Hits, bool IsDirectHit);

    public interface IVectorStoreService
    {
        Task IndexFaqAsync(List<FaqEntry> entries);
        Task IndexFactsAsync(List<FactEntry> entries);
        Task<FaqRetrievalResult> SearchFaqAsync(string query, int topK = 3, float minScore = 0.5f);
        Task<List<FactEntry>> SearchFactsAsync(string query, float minScore = 0.4f);
    }
}
