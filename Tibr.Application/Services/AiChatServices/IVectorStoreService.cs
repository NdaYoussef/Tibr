namespace Tibr.Application.Services.AiChatServices
{
    public record FaqEntry(string Id, string Question, string Answer, string? AnswerAr = null, string? QuestionAr = null);

    public record FactEntry(string Id, string Content, string? ContentAr = null);

    public record ScoredFaq(FaqEntry Entry, float Score);

    public record ScoredFact(FactEntry Entry, float Score);

    public record FaqRetrievalResult(List<ScoredFaq> Hits, bool IsDirectHit);

    public record RagResult(string Text, float Score, string Source);

    public interface IVectorStoreService
    {
        Task IndexFaqAsync(List<FaqEntry> entries);
        Task IndexFactsAsync(List<FactEntry> entries);
        Task<FaqRetrievalResult> SearchFaqAsync(string query, int topK = 3, float minScore = 0.5f);
        Task<List<ScoredFact>> SearchFactsAsync(string query, int topK = 3, float minScore = 0.4f);
    }
}
