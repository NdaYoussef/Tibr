using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class CartItem : BaseEntity<long>
    {
        public long CartId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public virtual Cart Cart { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
