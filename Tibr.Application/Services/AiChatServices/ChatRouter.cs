using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Application.Dtos.ChatDtos;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.InvestmentOrderServices;
using Tibr.Application.Services.PlanServices;
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
        private readonly IPlanService _planService;

        public ChatRouter(
            IAiProviderService aiProvider,
            IVectorStoreService vectorStore,
            IWalletService walletService,
            IAssetPriceService priceService,
            IGenericRepository<Trade, long> tradeRepo,
            IChatOrderProposalService proposalService,
            IInvestmentOrderService investmentOrderService,
            GoalParser goalParser,
            IPlanService planService
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
            _planService = planService;
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
            {
                var answer = language == "ar" && result.Hits[0].Entry.AnswerAr is not null
                    ? result.Hits[0].Entry.AnswerAr!
                    : result.Hits[0].Entry.Answer;
                return (answer, "system");
            }

            var context = string.Join(
                "\n\n",
                result.Hits.Select(h =>
                    h.Entry.QuestionAr is not null
                        ? $"Q: {h.Entry.Question}\nA: {h.Entry.Answer}\nس: {h.Entry.QuestionAr}\nج: {h.Entry.AnswerAr ?? h.Entry.Answer}  (score: {h.Score:F2})"
                        : $"Q: {h.Entry.Question}\nA: {h.Entry.Answer}  (score: {h.Score:F2})"
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
            var detected = DetectAssets(userPrompt);
            var assetTypes = detected.Count > 0 ? detected : [AssetType.Gold, AssetType.Silver];
            var wasAmbiguous = detected.Count == 0;

            var priceCtx = await BuildCombinedPriceContext(assetTypes, language);

            var system = "You are a helpful assistant for Tibr. "
                + "Answer the user's price-related question using the provided price context. Be concise. ";
            if (wasAmbiguous)
                system += language == "ar"
                    ? "إذا لم يكن واضحاً أي معدن يقصده المستخدم، اسأله قبل أن تفترض."
                    : "If it's unclear which metal the user means, ask before assuming. ";
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

        public async Task<(string Reply, string Source, bool ClarificationNeeded)> HandlePlannerAsync(
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
                    "system",
                    true
                );

            var balanceResult = await _walletService.GetBalancesAsync(userId);

            GoalMathResult mathResult;
            string assetsPrompt;
            decimal creationPrice = 0;
            decimal? silverCreationPrice = null;
            var goalTypeLabel = goal.GoalType switch
            {
                "reach_grams" => goal.Asset == "both"
                    ? $"own {goal.TargetAmount:F2}g total (split between Gold and Silver)"
                    : $"own {goal.TargetAmount:F2}g of {goal.Asset}",
                "reach_value_egp" => $"a portfolio worth {goal.TargetAmount:N0} EGP",
                "monthly_budget" => $"invest {goal.TargetAmount:N0} EGP/month",
                _ => "your goal",
            };

            if (goal.Asset == "both")
            {
                var goldPriceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);
                var silverPriceResult = await _priceService.GetCurrentPriceAsync(AssetType.Silver);

                var goldGrams = balanceResult.IsSuccess
                    ? balanceResult.Data!.FirstOrDefault(b => b.WalletType == WalletType.Gold)?.AvailableBalance ?? 0
                    : 0;
                var silverGrams = balanceResult.IsSuccess
                    ? balanceResult.Data!.FirstOrDefault(b => b.WalletType == WalletType.Silver)?.AvailableBalance ?? 0
                    : 0;
                var goldPrice = goldPriceResult.IsSuccess && goldPriceResult.Data is not null ? goldPriceResult.Data.SellPrice : 0;
                var silverPrice = silverPriceResult.IsSuccess && silverPriceResult.Data is not null ? silverPriceResult.Data.SellPrice : 0;
                creationPrice = goldPrice;
                silverCreationPrice = silverPrice;

                mathResult = GoalMathService.Compute(goal, new List<AssetInput>
                {
                    new("Gold", goldGrams, goldPrice),
                    new("Silver", silverGrams, silverPrice)
                });

                assetsPrompt = $"Current holdings: {goldGrams:F4}g Gold, {silverGrams:F4}g Silver\n"
                            + $"Current prices: Gold {goldPrice:N2} EGP/g, Silver {silverPrice:N2} EGP/g\n";
            }
            else
            {
                var assetType = goal.Asset?.ToLower() == "silver" ? AssetType.Silver : AssetType.Gold;
                var walletType = goal.Asset?.ToLower() == "silver" ? WalletType.Silver : WalletType.Gold;
                var priceResult = await _priceService.GetCurrentPriceAsync(assetType);

                var currentGrams = balanceResult.IsSuccess
                    ? balanceResult.Data!.FirstOrDefault(b => b.WalletType == walletType)?.AvailableBalance ?? 0
                    : 0;
                var currentPrice = priceResult.IsSuccess && priceResult.Data is not null ? priceResult.Data.SellPrice : 0;
                creationPrice = currentPrice;

                mathResult = GoalMathService.Compute(goal, currentGrams, currentPrice);

                assetsPrompt = $"Current holdings: {currentGrams:F4}g {goal.Asset}\n"
                            + $"Current price: {currentPrice:N2} EGP/g\n";
            }

            var advisePrompt =
                $"You are a financial advisor for Tibr. "
                + $"The user wants to achieve {goalTypeLabel} within {goal.TimeframeWeeks} weeks.\n"
                + assetsPrompt
                + $"Required weekly investment: {mathResult.RequiredWeeklyEgp:N2} EGP\n"
                + $"Projected completion: {mathResult.ProjectedCompletionDate:yyyy-MM-dd}\n\n"
                + $"Provide encouraging, practical advice. Do NOT recompute any numbers yourself."
                + " Do NOT ask follow-up questions — the user cannot answer in this chat."
                + " At the end, suggest the user can ask about what Tibr offers by saying 'what features does Tibr offer'.";

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

            // Persist plan after generating advice
            var planResult = await _planService.CreateFromGoalAsync(userId, goal, creationPrice, silverCreationPrice, language);

            if (adviceResponse.Content is not null)
                return (adviceResponse.Content, "ai", false);
            return (SystemMessages.PlannerFallback(language), "system", false);
        }

        public async Task<(string Reply, string Source, bool ClarificationNeeded)> HandlePlanUpdateAsync(
            long userId, string language)
        {
            var result = await _planService.ReevaluateAsync(userId, language);
            if (result.IsFailure)
                return (result.ErrorMessage ?? SystemMessages.PlanReevaluateFailed(language), "system", false);

            var data = result.Data!;
            var reply = data.Message;

            if (data.PriceChangePercent.HasValue && data.CurrentPrice.HasValue)
            {
                var metal = data.Plan.Asset == "silver" ? "Silver" : "Gold";
                reply += SystemMessages.PlanPriceMovement(language, metal, (double)data.PriceChangePercent.Value, data.Plan.PriceAtCreation, data.CurrentPrice.Value);
            }
            if (data.SilverPriceChangePercent.HasValue && data.SilverCurrentPrice.HasValue)
            {
                reply += SystemMessages.PlanSilverPriceMovement(language, (double)data.SilverPriceChangePercent.Value, data.Plan.SilverPriceAtCreation!.Value, data.SilverCurrentPrice.Value);
            }

            reply += SystemMessages.PlanReevaluateSuffix(language);
            return (reply, "system", false);
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

        private async Task<string> BuildCombinedPriceContext(List<AssetType> assetTypes, string language)
        {
            var parts = new List<string>();
            foreach (var assetType in assetTypes)
            {
                var priceResult = await _priceService.GetCurrentPriceAsync(assetType);
                if (!priceResult.IsSuccess || priceResult.Data is null) continue;

                var p = priceResult.Data;
                var analytics = await _priceService.GetPriceAnalyticsAsync(assetType);
                parts.Add(BuildPriceContext(p, analytics.IsSuccess ? analytics.Data : null, assetType, language));
            }

            if (parts.Count == 0) return SystemMessages.PriceUnavailable(language);
            return string.Join("\n---\n", parts);
        }

        private static string BuildPriceContext(
            AssetPriceDto p, PriceAnalyticsDto? a, AssetType assetType, string language)
        {
            var metal = assetType == AssetType.Silver
                ? (language == "ar" ? "الفضة" : "silver")
                : (language == "ar" ? "الذهب" : "gold");

            var ctx = language == "ar"
                ? $"سعر {metal} الحالي: شراء بسعر {p.BuyPrice:N2} ج.م، بيع بسعر {p.SellPrice:N2} ج.م (آخر تحديث {p.CreatedAt:HH:mm} UTC)"
                : $"Current {metal} price: buy at {p.BuyPrice:N2} EGP, sell at {p.SellPrice:N2} EGP (as of {p.CreatedAt:HH:mm} UTC)";

            if (a is null) return ctx;

            ctx += language == "ar"
                ? $"\nمتوسط 30 يوم: {a.AvgPriceLast30Days:N2} ج.م"
                : $"\n30-day average: {a.AvgPriceLast30Days:N2} EGP";
            ctx += language == "ar"
                ? $"\nأدنى سعر 30 يوم: {a.MinPriceLast30Days:N2} ج.م"
                : $"\n30-day low: {a.MinPriceLast30Days:N2} EGP";
            ctx += language == "ar"
                ? $"\nأعلى سعر 30 يوم: {a.MaxPriceLast30Days:N2} ج.م"
                : $"\n30-day high: {a.MaxPriceLast30Days:N2} EGP";

            if (a.DaysOfData < 30)
                ctx += language == "ar"
                    ? $"\nبيانات متاحة: {a.DaysOfData} يوم"
                    : $"\nData available: {a.DaysOfData} days";

            var range = a.MaxPriceLast30Days - a.MinPriceLast30Days;
            if (range > 0 && a.DaysOfData >= 7)
            {
                var percentile = (a.CurrentPrice - a.MinPriceLast30Days) / range * 100;
                ctx += language == "ar"
                    ? $"\nالسعر الحالي عند النسبة المئوية {percentile:F0} من نطاق 30 يوم"
                    : $"\nCurrent price is at the {percentile:F0}th percentile of the 30-day range";
            }

            if (a.IsBelowAverage)
                ctx += language == "ar"
                    ? $"\nالسعر الحالي أقل من متوسط 30 يوم بنسبة {Math.Abs(a.PercentBelowAverage ?? 0):F1}%"
                    : $"\nCurrent price is {Math.Abs(a.PercentBelowAverage ?? 0):F1}% below the 30-day average";
            else if (a.PercentBelowAverage > 0)
                ctx += language == "ar"
                    ? $"\nالسعر الحالي أعلى من متوسط 30 يوم بنسبة {a.PercentBelowAverage:F1}%"
                    : $"\nCurrent price is {a.PercentBelowAverage:F1}% above the 30-day average";

            if (a.IsNearMonthlyLow)
                ctx += language == "ar"
                    ? "\nالسعر في أدنى 10% من نطاق آخر 30 يومًا (أدنى: " + a.MinPriceLast30Days + ", أعلى: " + a.MaxPriceLast30Days + ", متوسط: " + a.AvgPriceLast30Days + "). هذه ملاحظة واقعية — وليست توصية أو توقع."
                    : $"\nPrice position: current price ({a.CurrentPrice:N2} EGP/g) is in the bottom 10% of the 30-day range (low: {a.MinPriceLast30Days:N2}, high: {a.MaxPriceLast30Days:N2}, avg: {a.AvgPriceLast30Days:N2}). State this as a 30-day observation only — not a buy signal, not a prediction. Example: 'Gold is near its 30-day low, though this doesn't indicate future direction.'";

            return ctx;
        }

        private static List<AssetType> DetectAssets(string prompt)
        {
            var lower = prompt.ToLowerInvariant();
            var detected = new List<AssetType>();
            if (lower.Contains("ذهب") || lower.Contains("دهب") || lower.Contains("gold") || lower.Contains("xau"))
                detected.Add(AssetType.Gold);
            if (lower.Contains("فضة") || lower.Contains("فضه") || lower.Contains("silver") || lower.Contains("xag"))
                detected.Add(AssetType.Silver);
            return detected;
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
