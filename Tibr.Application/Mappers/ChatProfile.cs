using Mapster;
using Tibr.Application.Dtos.ChatDtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class ChatProfile : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<ChatConversation, ConversationSummaryDto>()
                .Map(dest => dest.MessageCount, src => src.Messages.Count)
                .Map(dest => dest.LastMessage, src =>
                    src.Messages.OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content)
                        .FirstOrDefault())
                .Map(dest => dest.UpdatedAt, src =>
                    src.Messages.Any()
                        ? src.Messages.Max(m => m.CreatedAt)
                        : src.CreatedAt);

            config.NewConfig<ChatConversation, ConversationDetailDto>();

            config.NewConfig<ChatMessage, ChatMessageDto>()
                .Map(dest => dest.Role, src => src.Role.ToString());
        }
    }
}
