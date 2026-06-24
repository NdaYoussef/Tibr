using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities;
public class Review : BaseEntity<long>
{
    public long OrderId { get; set; }
    public long UserId { get; set; }
    public string? Description { get; set; }
    public int Value { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
