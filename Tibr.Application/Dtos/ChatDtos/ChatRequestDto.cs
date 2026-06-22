namespace Tibr.Application.Dtos.ChatDtos
{
    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public long? ConversationId { get; set; }
        public string? Intent { get; set; }
    }
}
