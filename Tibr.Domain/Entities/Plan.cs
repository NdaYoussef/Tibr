using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class Plan : BaseEntity<long>
    {
        public long UserId { get; set; }
        public User User { get; set; } = default!;

        public string GoalType { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public int? TimeframeWeeks { get; set; }
        public decimal? MonthlyBudgetEgp { get; set; }
        public decimal PriceAtCreation { get; set; }
        public decimal? SilverPriceAtCreation { get; set; }
        public DateTime? LastReevaluatedAt { get; set; }
        public PlanStatus Status { get; set; } = PlanStatus.Active;
    }
}
