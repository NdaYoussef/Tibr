using System.Collections.Generic;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class SupportTicket : BaseEntity<long>
    {
        public long UserId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public virtual User User { get; set; } = null!;
        public virtual ICollection<TicketReply> TicketReplies { get; set; } = [];
    }
}
