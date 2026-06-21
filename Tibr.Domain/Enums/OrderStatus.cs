using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 1,
        Triggered = 2,
        Executed = 3,
        Cancelled = 4,
        Failed = 5,
        Expired = 6
    }
}
