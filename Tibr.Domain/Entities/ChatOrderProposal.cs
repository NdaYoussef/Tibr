using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class ChatOrderProposal : BaseEntity<long>
    {
        public long ConversationId { get; set; }
        public ChatConversation Conversation { get; set; } = null!;
        public string ProposalJson { get; set; } = string.Empty;
        public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
        public DateTime ExpiresAt { get; set; }
    }
}
