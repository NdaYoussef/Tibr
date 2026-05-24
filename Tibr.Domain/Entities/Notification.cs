using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Notification : BaseEntity<long>
    {
        public long UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
