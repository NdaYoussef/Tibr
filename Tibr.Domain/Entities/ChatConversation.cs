using System.Collections.Generic;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class ChatConversation : BaseEntity<long>
    {
        public long UserId { get; set; }
        public string? Title { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<ChatMessage> Messages { get; set; } = [];
    }
}
