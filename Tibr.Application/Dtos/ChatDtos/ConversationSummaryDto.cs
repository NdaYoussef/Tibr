namespace Tibr.Application.Dtos.ChatDtos
{
    public class ConversationSummaryDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? LastMessage { get; set; }
        public int MessageCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
