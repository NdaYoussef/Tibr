using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class OrderItem : BaseEntity<long>
    {
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
