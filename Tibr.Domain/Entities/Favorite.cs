using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Favorite : BaseEntity<long>
    {
        public long UserId { get; set; }
        public long ProductId { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
