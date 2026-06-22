namespace Tibr.Application.Dtos.ChatDtos
{
    public class ChatResponseDto
    {
        public string Reply { get; set; } = string.Empty;
        public long ConversationId { get; set; }
        public string Intent { get; set; } = string.Empty;
    }
}
