using Tibr.Application.Dtos.ChatDtos;

namespace Tibr.Application.Services.AiChatServices
{
    public record AssetInput(string Asset, decimal CurrentGrams, decimal CurrentPrice);

    public static class GoalMathService
    {
        public static GoalMathResult Compute(GoalParseResult goal, decimal currentGrams, decimal currentPricePerGram)
        {
            return Compute(goal, new List<AssetInput>
            {
                new(goal.Asset, currentGrams, currentPricePerGram)
            });
        }

        public static GoalMathResult Compute(GoalParseResult goal, List<AssetInput> inputs)
        {
            var weeks = goal.TimeframeWeeks > 0 ? goal.TimeframeWeeks : 1;
            var totalRequiredWeekly = 0m;
            var breakdowns = new List<AssetBreakdown>();
            var maxDate = DateTime.UtcNow;

            foreach (var input in inputs)
            {
                var share = inputs.Count > 1 ? goal.TargetAmount / inputs.Count : goal.TargetAmount;

                var currentValueEgp = input.CurrentGrams * input.CurrentPrice;
                var targetValueEgp = goal.GoalType switch
                {
                    "reach_grams" => share * input.CurrentPrice,
                    "reach_value_egp" => share,
                    "monthly_budget" => share * (weeks / 4.0m),
                    _ => share
                };

                var gap = targetValueEgp - currentValueEgp;
                if (gap <= 0)
                {
                    breakdowns.Add(new AssetBreakdown(input.Asset, input.CurrentGrams, input.CurrentPrice, currentValueEgp, 0));
                    continue;
                }

                var requiredWeekly = gap / weeks;
                totalRequiredWeekly += requiredWeekly;
                var projectedDate = DateTime.UtcNow.AddDays(weeks * 7);
                if (projectedDate > maxDate) maxDate = projectedDate;

                breakdowns.Add(new AssetBreakdown(input.Asset, input.CurrentGrams, input.CurrentPrice, currentValueEgp, Math.Round(requiredWeekly, 2)));
            }

            return new GoalMathResult(
                Math.Round(totalRequiredWeekly, 2),
                maxDate,
                breakdowns
            );
        }
    }
}
