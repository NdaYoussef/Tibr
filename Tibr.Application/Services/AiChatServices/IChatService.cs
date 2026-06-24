using Tibr.Application.Dtos.ChatDtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.AiChatServices
{
    public interface IChatService
    {
        Task<Result<ChatResponseDto>> SendMessageAsync(long userId, ChatRequestDto request);
        Task<Result<List<ConversationSummaryDto>>> GetConversationsAsync(long userId);
        Task<Result<ConversationDetailDto>> GetConversationAsync(long userId, long conversationId);
        Task<Result> DeleteConversationAsync(long userId, long conversationId);
    }
}
