using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Favorite : BaseEntity<long>
    {
        public long UserId { get; set; }
        public long ProductId { get; set; }

        public  User User { get; set; } = null!;
        public  Product Product { get; set; } = null!;
    }
}
