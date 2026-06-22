using Tibr.Application.Dtos.ChatDtos;

namespace Tibr.Application.Services.AiChatServices
{
    public static class GoalMathService
    {
        public static GoalMathResult Compute(GoalParseResult goal, decimal currentGrams, decimal currentPricePerGram)
        {
            var currentValueEgp = currentGrams * currentPricePerGram;
            var targetValueEgp = goal.GoalType switch
            {
                "reach_grams" => goal.TargetAmount * currentPricePerGram,
                "reach_value_egp" => goal.TargetAmount,
                "monthly_budget" => goal.TargetAmount * (goal.TimeframeWeeks / 4.0m),
                _ => goal.TargetAmount
            };

            var gap = targetValueEgp - currentValueEgp;
            if (gap <= 0)
            {
                var completionDate = DateTime.UtcNow;
                return new GoalMathResult(0, completionDate);
            }

            var weeks = goal.TimeframeWeeks > 0 ? goal.TimeframeWeeks : 1;
            var requiredWeeklyEgp = gap / weeks;
            var projectedDate = DateTime.UtcNow.AddDays(weeks * 7);

            return new GoalMathResult(
                Math.Round(requiredWeeklyEgp, 2),
                projectedDate
            );
        }
    }
}
