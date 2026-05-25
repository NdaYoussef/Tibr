using System.Collections.Generic;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Admin : BaseEntity<long>
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public virtual ICollection<KYCDocument> ReviewedDocuments { get; set; } = [];
        public virtual ICollection<TicketReply> TicketReplies { get; set; } = [];
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = [];
    }
}
