using System;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Ticket : BaseEntity<long>
    {
        public long TicketId { get; set; }
        public long AdminId { get; set; }
        public string Message { get; set; } = string.Empty;

        public virtual Support Support { get; set; } = null!;
        public virtual Admin Admin { get; set; } = null!;
    }
}
