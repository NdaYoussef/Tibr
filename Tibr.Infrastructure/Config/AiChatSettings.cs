namespace Tibr.Infrastructure.Config
{
    public class AiChatSettings
    {
        public const string SectionName = "AiChat";
        public string ChatProvider { get; set; } = "gemini";
        public string EmbeddingProvider { get; set; } = "gemini";
        public string ChatApiKey { get; set; } = string.Empty;
        public string EmbeddingApiKey { get; set; } = string.Empty;
        public string ChatModel { get; set; } = "gemini-2.0-flash";
        public string EmbeddingModel { get; set; } = "gemini-embedding-2";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
        public string ChatBaseUrl { get; set; } = "https://api.openai.com/v1";
        public string EmbeddingBaseUrl { get; set; } = string.Empty;

        public string GetEffectiveEmbeddingApiKey() =>
            string.IsNullOrEmpty(EmbeddingApiKey)
                ? (EmbeddingProvider == "gemini" ? ChatApiKey : string.Empty)
                : EmbeddingApiKey;
    }
}
