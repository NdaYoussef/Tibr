using System.Text.Json;

namespace Tibr.Application.Services.AiChatServices
{
    public class IntentClassifier
    {
        private readonly IAiProviderService _aiProvider;

        private const string SystemPrompt = """
            You are an intent classifier for Tibr, a fractional gold investment app.
            Classify the user's message into exactly one intent and return ONLY valid JSON.

            Intents:
            - "faq": questions about how Tibr works, fractional investment, gold concepts
            - "facts": questions about Tibr's specific policies, fees, limits, rules
            - "price": questions about current gold prices or market context
            - "portfolio_read": questions about the user's own holdings or profitability (ANALYZE only, no action)
            - "planner": user wants to set a savings goal or simulate a scenario
            - "agentic": requests to buy/sell/trade gold or silver NOW. Examples: "buy 10g gold", "sell my silver", "place an order", "make a trade", "sell profitable fractions", "execute an order"
            - "conditional_order": user wants to set a condition on a future price before buying or selling. Examples: "buy when gold drops below 8000", "sell silver if it reaches 140", "set a strategy for gold", "alert me when gold hits 8500"
            - "out_of_scope": anything unrelated to gold, investment, or Tibr

            Priority rule: if the user asks to execute, perform, or make a trade/order, classify as "agentic", not "portfolio_read". The phrase "profitable fractions" in context of selling is agentic, not a portfolio query. If the user mentions a future price condition, classify as "conditional_order", not "agentic".

            Also detect the language of the user's message. Use ISO 639-1 codes (e.g. "en" for English, "ar" for Arabic).

            Return format:
            {
              "intent": "<intent>",
              "confidence": <0.0-1.0>,
              "reason": "<one sentence>",
              "language": "<ISO 639-1 code>"
            }

            Rules:
            - If confidence < 0.6, classify as out_of_scope
            - Never return more than one intent
            - Never add explanation outside the JSON
            """;

        public IntentClassifier(IAiProviderService aiProvider)
        {
            _aiProvider = aiProvider;
        }

        public async Task<ClassificationResult> ClassifyAsync(string userPrompt)
        {
            var history = new List<Message> { new("user", userPrompt) };
            var response = await _aiProvider.ChatAsync(SystemPrompt, history);

            var raw = response.Content ?? "{}";
            raw = raw.Trim();
            if (raw.StartsWith("```"))
                raw = string.Join('\n', raw.Split('\n')[1..^1]);

            try
            {
                var json = JsonDocument.Parse(raw).RootElement;
                var intentStr = json.GetProperty("intent").GetString() ?? "out_of_scope";
                var confidence = json.GetProperty("confidence").GetDouble();
                var reason = json.GetProperty("reason").GetString() ?? "";

                var intent = intentStr switch
                {
                    "faq" => Intent.Faq,
                    "facts" => Intent.Facts,
                    "price" => Intent.Price,
                    "portfolio_read" => Intent.PortfolioRead,
                    "planner" => Intent.Planner,
                    "agentic" => Intent.Agentic,
                    "conditional_order" => Intent.ConditionalOrder,
                    _ => Intent.OutOfScope,
                };

                var language = json.TryGetProperty("language", out var langEl)
                    ? langEl.GetString() ?? "en"
                    : "en";

                return new ClassificationResult(intent, confidence, reason)
                {
                    Language = language
                };
            }
            catch
            {
                return new ClassificationResult(Intent.OutOfScope, 0, "Failed to parse classifier response");
            }
        }
    }
}
