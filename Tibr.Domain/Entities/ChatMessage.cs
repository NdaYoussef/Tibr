using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class ChatMessage : BaseEntity<long>
    {
        public long ConversationId { get; set; }
        public ChatRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Metadata { get; set; }

        public ChatConversation Conversation { get; set; } = null!;
    }
}
