using System.Collections.Generic;
using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Product : BaseEntity<long>
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public MetalType MetalType { get; set; }
        public decimal Purity { get; set; }
        public decimal Weight { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public decimal Stock { get; set; } = 0;
        public string? ImageUrl { get; set; } = string.Empty;
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<Favorite> Favorites { get; set; } = [];
        public virtual ICollection<CartItem> CartItems { get; set; } = [];
        public virtual ICollection<OrderItem> OrderItems { get; set; } = [];
    }
}
