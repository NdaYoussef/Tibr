using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Domain.Common.Classes
{
    public class BaseEntity<T>
    {
        public required T Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // public T? CreatedById { get; set; }
        // public T? UpdatedById { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
