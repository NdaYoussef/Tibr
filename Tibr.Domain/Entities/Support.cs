using System.Collections.Generic;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Support : BaseEntity<long>
    {
        public long UserId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public SupportStatus Status { get; set; } = SupportStatus.Open;

        public  User User { get; set; } = null!;
        public  ICollection<Ticket> Tickets{ get; set; } = [];
        public enum SupportStatus
        {
            Open =1,
            Pending,
            Resolved,
            Closed
        }
    }
}
