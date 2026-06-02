using System;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Ticket : BaseEntity<long>
    {
        public long AdminId { get; set; }
        public long SupportId { get; set; }
        public string Message { get; set; } = string.Empty;

        public  Support Support { get; set; } = null!;
        public  Admin Admin { get; set; } = null!;
    }
}
