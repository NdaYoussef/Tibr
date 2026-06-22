namespace Tibr.Application.Dtos.ChatDtos
{
    public record GoalParseResult(
        string GoalType,
        string Asset,
        decimal TargetAmount,
        int TimeframeWeeks,
        bool ClarificationNeeded,
        string? ClarificationQuestion);
}
