namespace Tibr.Application.Dtos.ChatDtos
{
    public record AssetBreakdown(
        string Asset,
        decimal CurrentGrams,
        decimal CurrentPrice,
        decimal CurrentValueEgp,
        decimal RequiredWeeklyEgp
    );

    public record GoalMathResult(
        decimal RequiredWeeklyEgp,
        DateTime ProjectedCompletionDate,
        List<AssetBreakdown> Breakdowns
    );
}
