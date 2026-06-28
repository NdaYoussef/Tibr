using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Application.Dtos.ChatDtos;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.InvestmentOrderServices;
using Tibr.Application.Services.WalletServices;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;

namespace Tibr.Application.Services.AiChatServices
{
    public class ChatRouter
    {
        private readonly IAiProviderService _aiProvider;
        private readonly IVectorStoreService _vectorStore;
        private readonly IWalletService _walletService;
        private readonly IAssetPriceService _priceService;
        private readonly IGenericRepository<Trade, long> _tradeRepo;
        private readonly IChatOrderProposalService _proposalService;
        private readonly IInvestmentOrderService _investmentOrderService;
        private readonly GoalParser _goalParser;

        public ChatRouter(
            IAiProviderService aiProvider,
            IVectorStoreService vectorStore,
            IWalletService walletService,
            IAssetPriceService priceService,
            IGenericRepository<Trade, long> tradeRepo,
            IChatOrderProposalService proposalService,
            IInvestmentOrderService investmentOrderService,
            GoalParser goalParser
        )
        {
            _aiProvider = aiProvider;
            _vectorStore = vectorStore;
            _walletService = walletService;
            _priceService = priceService;
            _tradeRepo = tradeRepo;
            _proposalService = proposalService;
            _investmentOrderService = investmentOrderService;
            _goalParser = goalParser;
        }

        public (string Reply, string Source) HandleOutOfScope(string language)
        {
            return (SystemMessages.OutOfScope(language), "system");
        }

        public async Task<(string Reply, string Source)> HandleFaqAsync(
            string userPrompt,
            string language,
            List<Message> history
        )
        {
            var result = await _vectorStore.SearchFaqAsync(userPrompt);

            if (result.Hits.Count == 0)
                return (SystemMessages.FaqNoAnswers(language), "system");

            if (result.IsDirectHit)
                return (result.Hits[0].Entry.Answer, "system");

            var context = string.Join(
                "\n\n",
                result.Hits.Select(h =>
                    $"Q: {h.Entry.Question}\nA: {h.Entry.Answer}  (score: {h.Score:F2})"
                )
            );

            var system =
                "You are a helpful assistant for Tibr, a gold investment app. "
                + "Answer the user's question using ONLY the provided FAQ context. "
                + "Be concise and friendly.";
            system += language == "ar" ? "\nRespond in Arabic." : "\nRespond in English.";

            var messages = new List<Message>(history)
            {
                new("user", $"Context:\n{context}\n\nQuestion: {userPrompt}"),
            };

            var response = await _aiProvider.ChatAsync(system, messages);
            if (response.Content is not null)
                return (response.Content, "ai");
            return (SystemMessages.FaqGenFailed(language), "system");
        }

        public async Task<(string Reply, string Source)> HandleFactsAsync(
            string userPrompt,
            string language,
            List<Message> history
        )
        {
            var facts = await _vectorStore.SearchFactsAsync(userPrompt);
            var priceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);

            var factsText =
                facts.Count > 0
                    ? string.Join("\n", facts.Select(f => $"- {f.Entry.Content}  (score: {f.Score:F2})"))
                    : SystemMessages.FactsNoFacts(language);

            var priceCtx =
                priceResult.IsSuccess && priceResult.Data is not null
                    ? $"Current gold price: buy at {priceResult.Data.BuyPrice:N2} EGP/g, "
                        + $"sell at {priceResult.Data.SellPrice:N2} EGP/g"
                    : SystemMessages.FactsPriceUnavailable(language);

            var system =
                "You are a helpful assistant for Tibr. "
                + "Answer using ONLY the provided facts and price context. Be concise.";
            system += language == "ar" ? "\nRespond in Arabic." : "\nRespond in English.";

            var messages = new List<Message>(history)
            {
                new(
                    "user",
                    $"Facts:\n{factsText}\n\nPrice context:\n{priceCtx}\n\nQuestion: {userPrompt}"
                ),
            };

            var response = await _aiProvider.ChatAsync(system, messages);
            if (response.Content is not null)
                return (response.Content, "ai");
            return (SystemMessages.FactsGenFailed(language), "system");
        }

        public async Task<(string Reply, string Source)> HandlePriceAsync(
            string userPrompt,
            string language,
            List<Message> history
        )
        {
            var priceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);

            if (!priceResult.IsSuccess || priceResult.Data is null)
                return (SystemMessages.PriceUnavailable(language), "system");

            var p = priceResult.Data;
            var priceCtx =
                language == "ar"
                    ? $"سعر الذهب الحالي: شراء بسعر {p.BuyPrice:N2} جنيها للجرام، بيع بسعر {p.SellPrice:N2} جنيها للجرام (حتى {p.CreatedAt:HH:mm} UTC)"
                    : $"Current gold price: buy at {p.BuyPrice:N2} EGP/g, sell at {p.SellPrice:N2} EGP/g (as of {p.CreatedAt:HH:mm} UTC)";

            var system =
                "You are a helpful assistant for Tibr. "
                + "Answer the user's price-related question using the provided price context. Be concise.";
            system += language == "ar" ? "\nRespond in Arabic." : "\nRespond in English.";

            var messages = new List<Message>(history)
            {
                new("user", $"Price context:\n{priceCtx}\n\nQuestion: {userPrompt}"),
            };

            var response = await _aiProvider.ChatAsync(system, messages);
            if (response.Content is not null)
                return (response.Content, "ai");
            return (priceCtx, "system");
        }

        public async Task<(string Reply, string Source)> HandlePortfolioReadAsync(
            string userPrompt,
            long userId,
            string language,
            List<Message> history
        )
        {
            var balanceResult = await _walletService.GetBalancesAsync(userId);
            var priceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);
            var trades = await _tradeRepo.GetAll(t => t.UserId == userId).ToListAsync();

            var goldBalance = balanceResult.IsSuccess
                ? (
                    balanceResult
                        .Data!.FirstOrDefault(b => b.WalletType == WalletType.Gold)
                        ?.AvailableBalance
                    ?? 0
                )
                : 0;

            var cashBalance = balanceResult.IsSuccess
                ? (
                    balanceResult
                        .Data!.FirstOrDefault(b => b.WalletType == WalletType.Cash)
                        ?.AvailableBalance
                    ?? 0
                )
                : 0;

            var priceCtx =
                priceResult.IsSuccess && priceResult.Data is not null
                    ? $"Current gold price: buy at {priceResult.Data.BuyPrice:N2} EGP/g, "
                        + $"sell at {priceResult.Data.SellPrice:N2} EGP/g"
                    : "Price data temporarily unavailable.";

            var buyTrades = trades
                .Where(t => t.Side == TradeSide.Buy)
                .OrderBy(t => t.ExecutedAt)
                .ToList();

            var tradeText =
                buyTrades.Count > 0
                    ? string.Join(
                        "\n",
                        buyTrades.Select(
                            (t, i) =>
                            {
                                var currentSellPrice =
                                    priceResult.IsSuccess && priceResult.Data is not null
                                        ? priceResult.Data.SellPrice
                                        : t.ExecutedPrice;
                                var plPerGram = currentSellPrice - t.ExecutedPrice;
                                var totalPl = plPerGram * t.Quantity;
                                return $"- Trade #{i + 1}: {t.Quantity:F4}g at {t.ExecutedPrice:N2} EGP/g "
                                    + $"on {t.ExecutedAt:yyyy-MM-dd}  "
                                    + $"P/L: {plPerGram:+N2;-N2} EGP/g, total: {totalPl:+N2;-N2} EGP";
                            }
                        )
                    )
                    : "No buy trades found.";

            var holdingsText =
                $"Gold balance: {goldBalance:F4}g\nCash balance: {cashBalance:N2} EGP";

            var system =
                "You are a financial assistant for Tibr. Analyze the user's portfolio "
                + "and answer their question. Be precise with numbers. Do not recommend actions unless asked.";
            system += language == "ar" ? "\nRespond in Arabic." : "\nRespond in English.";

            var messages = new List<Message>(history)
            {
                new(
                    "user",
                    $"Holdings:\n{holdingsText}\n\nTrades:\n{tradeText}\n\n"
                        + $"Price context:\n{priceCtx}\n\nQuestion: {userPrompt}"
                ),
            };

            var response = await _aiProvider.ChatAsync(system, messages);
            if (response.Content is not null)
                return (response.Content, "ai");
            return (SystemMessages.PortfolioGenFailed(language), "system");
        }

        public async Task<(
            string Reply,
            object? ToolCallRequest,
            string Source
        )> HandleAgenticAsync(string userPrompt, long userId, long conversationId, string language, List<Message> history)
        {
            var systemPrompt =
                "You are an order assistant for Tibr, a fractional gold investment app. "
                + "If the user wants to buy or sell gold or silver now, use the propose_order tool. "
                + "If the phrasing implies a conditional order (e.g., 'when price drops below', 'if it reaches'), "
                + "do NOT use propose_order — the conditional_order intent handles that separately.";
            systemPrompt += language == "ar" ? "\nRespond in Arabic." : "\nRespond in English.";

            var messages = new List<Message>(history) { new("user", userPrompt) };
            var tools = new List<object> { Tools.OrderBuilderTool.FunctionDeclaration };
            var response = await _aiProvider.ChatAsync(systemPrompt, messages, tools);

            if (response.ToolCalls is { Count: > 0 })
            {
                var call = response.ToolCalls[0];
                return ("", call, "system");
            }

            if (response.Content is not null)
                return (response.Content, null, "ai");
            return (SystemMessages.AgenticFallback(language), null, "system");
        }

        public async Task<(
            string Reply,
            object? ToolCallRequest,
            string Source
        )> HandleConditionalOrderAsync(string userPrompt, long userId, string language, List<Message> history)
        {
            var systemPrompt =
                "You are a strategy assistant for Tibr, a fractional gold investment app. "
                + "If the user wants to set a conditional order (buy/sell when price reaches a target), "
                + "use the create_strategy_order tool. Capture asset, side (buy/sell), operator (greater_than/less_than), "
                + "target_price_egp, execution_type (alert_only/auto_execute), quantity_grams, and expires_in_days.";
            systemPrompt += language == "ar" ? "\nRespond in Arabic." : "\nRespond in English.";

            var messages = new List<Message>(history) { new("user", userPrompt) };
            var tools = new List<object>
            {
                Tools.OrderBuilderTool.CreateStrategyFunctionDeclaration,
            };
            var response = await _aiProvider.ChatAsync(systemPrompt, messages, tools);

            if (response.ToolCalls is { Count: > 0 })
            {
                var call = response.ToolCalls[0];
                return ("", call, "system");
            }

            if (response.Content is not null)
                return (response.Content, null, "ai");
            return (SystemMessages.ConditionalFallback(language), null, "system");
        }

        public async Task<(string Reply, string Source)> HandlePlannerAsync(
            string userPrompt,
            long userId,
            string language,
            List<Message> history
        )
        {
            var goal = await _goalParser.ParseAsync(userPrompt, language);

            if (goal.ClarificationNeeded)
                return (
                    goal.ClarificationQuestion ?? SystemMessages.PlannerClarify(language),
                    "system"
                );

            var balanceResult = await _walletService.GetBalancesAsync(userId);
            var priceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);

            var currentGrams = balanceResult.IsSuccess
                ? (
                    balanceResult
                        .Data!.FirstOrDefault(b => b.WalletType == WalletType.Gold)
                        ?.AvailableBalance
                    ?? 0
                )
                : 0;

            var currentPrice =
                priceResult.IsSuccess && priceResult.Data is not null
                    ? priceResult.Data.SellPrice
                    : 0;

            var mathResult = GoalMathService.Compute(goal, currentGrams, currentPrice);

            var goalTypeLabel = goal.GoalType switch
            {
                "reach_grams" => $"own {goal.TargetAmount:F2}g of {goal.Asset}",
                "reach_value_egp" => $"a portfolio worth {goal.TargetAmount:N0} EGP",
                "monthly_budget" => $"invest {goal.TargetAmount:N0} EGP/month",
                _ => "your goal",
            };

            var advisePrompt =
                $"You are a financial advisor for Tibr. "
                + $"The user wants to achieve {goalTypeLabel} within {goal.TimeframeWeeks} weeks.\n"
                + $"Current holdings: {currentGrams:F4}g {goal.Asset}\n"
                + $"Current price: {currentPrice:N2} EGP/g\n"
                + $"Required weekly investment: {mathResult.RequiredWeeklyEgp:N2} EGP\n"
                + $"Projected completion: {mathResult.ProjectedCompletionDate:yyyy-MM-dd}\n\n"
                + $"Provide encouraging, practical advice. Do NOT recompute any numbers yourself.";

            var facts = await _vectorStore.SearchFactsAsync(userPrompt, topK: 2, minScore: 0.4f);
            var factsText =
                facts.Count > 0
                    ? "\n\nRelevant policy facts:\n" + string.Join("\n", facts.Select(f => $"- {f.Entry.Content}"))
                    : "";

            advisePrompt += factsText;
            advisePrompt += language == "ar"
                ? "\nRespond in Arabic."
                : "\nRespond in English.";

            var messages = new List<Message>(history) { new("user", userPrompt) };
            var adviceResponse = await _aiProvider.ChatAsync(advisePrompt, messages);
            if (adviceResponse.Content is not null)
                return (adviceResponse.Content, "ai");
            return (SystemMessages.PlannerFallback(language), "system");
        }

        public async Task<(string Reply, string Source)> HandleDualRagAsync(
            string userPrompt, string language, int topK, List<Message> history)
        {
            var faq = await _vectorStore.SearchFaqAsync(userPrompt, topK, minScore: 0.5f);
            var facts = await _vectorStore.SearchFactsAsync(userPrompt, topK, minScore: 0.4f);

            var merged = MergeAndRank(faq, facts, topK);

            if (merged.Count == 0)
                return (SystemMessages.FaqNoAnswers(language), "system");

            var context = string.Join(
                "\n\n",
                merged.Select(r => $"[{r.Source}] {r.Text}  (score: {r.Score:F2})"));

            var system = "You are a helpful assistant for Tibr. "
                + "Answer using ONLY the provided context. Be concise and friendly.";
            system += language == "ar" ? "\nRespond in Arabic." : "\nRespond in English.";

            var messages = new List<Message>(history)
            {
                new("user", $"Context:\n{context}\n\nQuestion: {userPrompt}")
            };

            var response = await _aiProvider.ChatAsync(system, messages);
            return (response.Content ?? SystemMessages.FaqGenFailed(language), "system");
        }

        private static List<RagResult> MergeAndRank(
            FaqRetrievalResult faq, List<ScoredFact> facts, int topK)
        {
            var results = new List<RagResult>();

            foreach (var hit in faq.Hits)
                results.Add(new RagResult(
                    $"Q: {hit.Entry.Question}\nA: {hit.Entry.Answer}",
                    hit.Score, "FAQ"));

            foreach (var hit in facts)
                results.Add(new RagResult(hit.Entry.Content, hit.Score, "Facts"));

            return results.OrderByDescending(r => r.Score).Take(topK * 2).ToList();
        }
    }
}
