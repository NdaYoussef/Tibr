using System.Collections.Generic;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Cart : BaseEntity<long>
    {
        public long UserId { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual ICollection<CartItem> CartItems { get; set; } = [];
    }
}
