namespace Tibr.Application.Dtos.ChatDtos
{
    public class ConversationDetailDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = [];
    }
}
