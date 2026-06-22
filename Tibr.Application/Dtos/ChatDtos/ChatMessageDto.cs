namespace Tibr.Application.Dtos.ChatDtos
{
    public class ChatMessageDto
    {
        public long Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
