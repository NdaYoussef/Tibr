using System.Text.Json;
using Tibr.Application.Dtos.ChatDtos;

namespace Tibr.Application.Services.AiChatServices
{
    public class GoalParser
    {
        private readonly IAiProviderService _aiProvider;

        private const string SystemPrompt = """
            You are a goal parser for Tibr, a fractional gold investment app.
            Extract structured savings goal information from the user's message and return ONLY valid JSON.

            Return format:
            {
              "goal_type": "reach_grams" | "reach_value_egp" | "monthly_budget",
              "asset": "gold" | "silver" | "both",
              "target_amount": number,
              "timeframe_weeks": number,
              "clarification_needed": false,
              "clarification_question": null
            }

            If the user's goal is ambiguous (missing amount, timeframe, or asset), set
            "clarification_needed": true and provide a clarification_question.

            Rules:
            - "reach_grams": user wants to own a specific gram amount
            - "reach_value_egp": user wants their portfolio to be worth a specific EGP amount
            - "monthly_budget": user wants to invest a fixed amount per month
            - Never return more than one goal type
            - Never add explanation outside the JSON
            """;

        public GoalParser(IAiProviderService aiProvider)
        {
            _aiProvider = aiProvider;
        }

            public async Task<GoalParseResult> ParseAsync(string userMessage, string language = "en")
            {
                var langInstruction = language == "ar"
                    ? "Respond to the user in Arabic. The clarification_question must be in Arabic."
                    : "Respond to the user in English.";
                var prompt = SystemPrompt + "\n\n" + langInstruction;
                var history = new List<Message> { new("user", userMessage) };
                var response = await _aiProvider.ChatAsync(prompt, history);

            var raw = (response.Content ?? "{}").Trim();
            if (raw.StartsWith("```"))
                raw = string.Join('\n', raw.Split('\n')[1..^1]);

            try
            {
                var json = JsonDocument.Parse(raw).RootElement;
                var clarificationNeeded = json.GetProperty("clarification_needed").GetBoolean();

                if (clarificationNeeded)
                {
                    return new GoalParseResult(
                        GoalType: "",
                        Asset: "",
                        TargetAmount: 0,
                        TimeframeWeeks: 0,
                        ClarificationNeeded: true,
                        ClarificationQuestion: json.GetProperty("clarification_question").GetString()
                    );
                }

                return new GoalParseResult(
                    GoalType: json.GetProperty("goal_type").GetString() ?? "",
                    Asset: json.GetProperty("asset").GetString() ?? "gold",
                    TargetAmount: json.GetProperty("target_amount").GetDecimal(),
                    TimeframeWeeks: json.GetProperty("timeframe_weeks").GetInt32(),
                    ClarificationNeeded: false,
                    ClarificationQuestion: null
                );
            }
            catch
            {
                var fallbackMsg = language == "ar"
                    ? "لتقديم خطة ادخار، أحتاج إلى: المبلغ المستهدف (بالجرام أو الجنيه)، نوع المعدن (ذهب أو فضة)، والمدة الزمنية بالأسابيع أو الأشهر."
                    : "To create a savings plan, I need: the target amount (in grams or EGP), the metal type (gold or silver), and the time period in weeks or months.";
                return new GoalParseResult(
                    GoalType: "", Asset: "", TargetAmount: 0,
                    TimeframeWeeks: 0, ClarificationNeeded: true,
                    ClarificationQuestion: fallbackMsg
                );
            }
        }
    }
}
