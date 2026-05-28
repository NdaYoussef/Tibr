using System;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class AuditLog : BaseEntity<long>
    {
        public long AdminId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public long RecordId { get; set; }

        public virtual Admin Admin { get; set; } = null!;
    }
}
