namespace Tibr.Application.Dtos
{
    public class PlanDto
    {
        public long Id { get; set; }
        public string GoalType { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public int? TimeframeWeeks { get; set; }
        public decimal? MonthlyBudgetEgp { get; set; }
        public decimal PriceAtCreation { get; set; }
        public decimal? SilverPriceAtCreation { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastReevaluatedAt { get; set; }
    }

    public class CreatePlanDto
    {
        public string GoalType { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public int? TimeframeWeeks { get; set; }
        public decimal? MonthlyBudgetEgp { get; set; }
    }

    public class ReevaluatePlanResultDto
    {
        public PlanDto Plan { get; set; } = null!;
        public decimal? CurrentPrice { get; set; }
        public decimal? SilverCurrentPrice { get; set; }
        public decimal WalletBalance { get; set; }
        public decimal? SilverWalletBalance { get; set; }
        public decimal ProgressPercent { get; set; }
        public decimal? PriceChangePercent { get; set; }
        public decimal? SilverPriceChangePercent { get; set; }
        public decimal? RequiredWeeklyEgp { get; set; }
        public int? RemainingWeeks { get; set; }
        public decimal? InvestedSinceStart { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Completed { get; set; }
    }

    public class ExtendDeadlineDto
    {
        public int NewTimeframeWeeks { get; set; }
    }

}
