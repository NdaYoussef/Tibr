using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities;
public class Withdraw : BaseEntity<long>
{
    public decimal Amount { get; set; }
    public WithdrawType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public long UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
