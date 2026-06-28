using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Tibr.Application.Services.AiChatServices
{
    public class IntentClassifier
    {
        private readonly IAiProviderService _aiProvider;
        private readonly ILogger<IntentClassifier> _logger;

        private const string SystemPrompt = """
            You are an intent classifier for Tibr, a fractional gold investment app.
            Classify the user's message into exactly one intent and return ONLY valid JSON.
            The user may write in English or Arabic — respond with the correct intent regardless.

            Intents with examples in both languages:

            - "price": questions about current prices or market timing
              AR: سعر الذهب, سعر الفضة, كم سعر الذهب اليوم, هل الوقت مناسب للشراء, أسعار اليوم
              EN: gold price, silver price, current rate, is now a good time to buy, market price

            - "faq": questions about how Tibr works, fractional investment, general gold concepts
              AR: كيف يعمل تبر, ما هو, شرح, كيف أبدأ, ما معنى الاستثمار الجزئي
              EN: how does Tibr work, what is fractional, explain, minimum investment, how to start

            - "facts": questions about Tibr's specific policies, fees, limits, rules
              AR: رسوم, سياسة, حد أدنى, عمولة, مصاريف, شروط
              EN: fees, policy, limits, rules, commission, charges

            - "portfolio_read": questions about the user's own holdings or profitability (ANALYZE only, no action)
              AR: محفظتي, أرباحي, كم عندي ذهب, رصيدي, مشترياتي, صافي الربح
              EN: my portfolio, my holdings, my profit, my balance, show my trades

            - "planner": user expresses a specific savings goal with amount + timeframe + asset
              AR: أريد ادخار 10 جرام ذهب في 3 شهور, أحتاج 20 جرام بحلول ديسمبر, وفر 500 جنيه شهرياً
              EN: I want to save 10g of gold in 3 months, help me invest 500 EGP per month, I want 20g by December
              IMPORTANT: Questions about inflation protection, market timing ("هل الوقت مناسب؟"), or general
              financial advice are NOT planner. Only messages with a clear target amount/timeframe/asset are planner.

            - "agentic": requests to buy/sell/trade gold or silver NOW
              AR: اشتري ذهب, بيع فضة, شراء سبائك, نفذ أمر, أريد شراء, أريد بيع
              EN: buy 10g gold, sell my silver, place an order, make a trade, execute an order, purchase gold

            - "conditional_order": user wants to set a condition on a future price before buying/selling
              AR: اشتري إذا انخفض السعر, بيع عندما يصل, أمر معلق, أوقف الخسارة
              EN: buy when gold drops below 8000, sell silver if it reaches 140, set a strategy, alert me when

            - "out_of_scope": anything unrelated to gold, investment, or Tibr

            CRITICAL NEGATIVE RULES — "planner" is STRICTLY for explicit savings goals:
            - Questions about prices or market timing → "price". NEVER planner.
            - Questions about inflation, economy, financial advice → "faq" or "facts". NEVER planner.
            - Requests to buy/sell → "agentic". NEVER planner.
            - Questions about how Tibr works → "faq". NEVER planner.
            - Greetings → "faq" or "out_of_scope". NEVER planner.
            - "هل الوقت مناسب للشراء؟", "Is now a good time?" → "price". NEVER planner.
            - "كيف أحمي مدخراتي", "How to protect savings" → "facts" or "faq". NEVER planner.

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

        public IntentClassifier(IAiProviderService aiProvider, ILogger<IntentClassifier> logger)
        {
            _aiProvider = aiProvider;
            _logger = logger;
        }

        public async Task<ClassificationResult> ClassifyAsync(string userPrompt)
        {
            var history = new List<Message> { new("user", userPrompt) };
            var response = await _aiProvider.ChatAsync(SystemPrompt, history);

            _logger.LogInformation(
                "Classifier raw response for \"{Message}\": {Raw}",
                userPrompt, response.Content);

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
                _logger.LogWarning("Failed to parse classifier JSON for \"{Message}\". Raw: {Raw}", userPrompt, raw);
                var reason = raw switch
                {
                    "I'm sorry, the AI service is temporarily unavailable. Please try again shortly." => "AI_SERVICE_UNAVAILABLE",
                    { Length: > 0 } and not "{}" => raw,
                    _ => "Failed to parse classifier response"
                };
                return new ClassificationResult(Intent.OutOfScope, 0, reason);
            }
        }
    }
}
