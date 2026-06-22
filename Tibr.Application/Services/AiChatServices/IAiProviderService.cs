namespace Tibr.Application.Services.AiChatServices
{
    public record Message(string Role, string Content);

    public record ToolCall(string Id, string FunctionName, string Arguments);

    public record AssistantResponse(string? Content, List<ToolCall>? ToolCalls);

    public interface IAiProviderService
    {
        Task<AssistantResponse> ChatAsync(
            string systemPrompt,
            List<Message> history,
            List<object>? tools = null);

        Task<List<float[]>> EmbedBatchAsync(List<string> texts);

        Task<float[]> EmbedAsync(string text);
    }
}
