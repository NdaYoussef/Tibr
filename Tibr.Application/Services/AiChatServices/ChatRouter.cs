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
            GoalParser goalParser)
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

        public string HandleOutOfScope() =>
            "I can only help with gold and silver investment topics on Tibr. "
            + "Feel free to ask about your portfolio, gold prices, or how Tibr works.";

        public async Task<string> HandleFaqAsync(string userPrompt)
        {
            var result = await _vectorStore.SearchFaqAsync(userPrompt);

            if (result.Hits.Count == 0)
                return "I don't have a specific answer for that. "
                    + "Try rephrasing or contact Tibr support.";

            if (result.IsDirectHit)
                return result.Hits[0].Entry.Answer;

            var context = string.Join(
                "\n\n",
                result.Hits.Select(h =>
                    $"Q: {h.Entry.Question}\nA: {h.Entry.Answer}  (score: {h.Score:F2})"));

            var system = "You are a helpful assistant for Tibr, a gold investment app. "
                + "Answer the user's question using ONLY the provided FAQ context. "
                + "Be concise and friendly.";

            var history = new List<Message>
            {
                new("user", $"Context:\n{context}\n\nQuestion: {userPrompt}")
            };

            var response = await _aiProvider.ChatAsync(system, history);
            return response.Content ?? "Sorry, I could not generate an answer.";
        }

        public async Task<string> HandleFactsAsync(string userPrompt)
        {
            var facts = await _vectorStore.SearchFactsAsync(userPrompt);
            var priceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);

            var factsText = facts.Count > 0
                ? string.Join("\n", facts.Select(f => $"- {f.Content}"))
                : "No specific policy facts found.";

            var priceCtx = priceResult.IsSuccess && priceResult.Data is not null
                ? $"Current gold price: buy at {priceResult.Data.BuyPrice:N2} EGP/g, "
                  + $"sell at {priceResult.Data.SellPrice:N2} EGP/g"
                : "Price data temporarily unavailable.";

            var system = "You are a helpful assistant for Tibr. "
                + "Answer using ONLY the provided facts and price context. Be concise.";

            var history = new List<Message>
            {
                new("user", $"Facts:\n{factsText}\n\nPrice context:\n{priceCtx}\n\nQuestion: {userPrompt}")
            };

            var response = await _aiProvider.ChatAsync(system, history);
            return response.Content ?? "Sorry, I could not generate an answer.";
        }

        public async Task<string> HandlePriceAsync(string userPrompt)
        {
            var priceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);

            if (!priceResult.IsSuccess || priceResult.Data is null)
                return "I'm unable to fetch the current gold price right now. Please try again later.";

            var p = priceResult.Data;
            var priceCtx = $"Current gold price: buy at {p.BuyPrice:N2} EGP/g, "
                + $"sell at {p.SellPrice:N2} EGP/g (as of {p.CreatedAt:HH:mm} UTC)";

            var system = "You are a helpful assistant for Tibr. "
                + "Answer the user's price-related question using the provided price context. Be concise.";

            var history = new List<Message>
            {
                new("user", $"Price context:\n{priceCtx}\n\nQuestion: {userPrompt}")
            };

            var response = await _aiProvider.ChatAsync(system, history);
            return response.Content ?? priceCtx;
        }

        public async Task<string> HandlePortfolioReadAsync(string userPrompt, long userId)
        {
            var balanceResult = await _walletService.GetBalancesAsync(userId);
            var priceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);
            var trades = await _tradeRepo.GetAll(t => t.UserId == userId).ToListAsync();

            var goldBalance = balanceResult.IsSuccess
                ? (balanceResult.Data!.FirstOrDefault(b => b.WalletType == WalletType.Gold)
                    ?.AvailableBalance ?? 0)
                : 0;

            var cashBalance = balanceResult.IsSuccess
                ? (balanceResult.Data!.FirstOrDefault(b => b.WalletType == WalletType.Cash)
                    ?.AvailableBalance ?? 0)
                : 0;

            var priceCtx = priceResult.IsSuccess && priceResult.Data is not null
                ? $"Current gold price: buy at {priceResult.Data.BuyPrice:N2} EGP/g, "
                  + $"sell at {priceResult.Data.SellPrice:N2} EGP/g"
                : "Price data temporarily unavailable.";

            var buyTrades = trades
                .Where(t => t.Side == TradeSide.Buy)
                .OrderBy(t => t.ExecutedAt)
                .ToList();

            var tradeText = buyTrades.Count > 0
                ? string.Join("\n", buyTrades.Select((t, i) =>
                  {
                      var currentSellPrice = priceResult.IsSuccess && priceResult.Data is not null
                          ? priceResult.Data.SellPrice : t.ExecutedPrice;
                      var plPerGram = currentSellPrice - t.ExecutedPrice;
                      var totalPl = plPerGram * t.Quantity;
                      return $"- Trade #{i + 1}: {t.Quantity:F4}g at {t.ExecutedPrice:N2} EGP/g "
                          + $"on {t.ExecutedAt:yyyy-MM-dd}  "
                          + $"P/L: {plPerGram:+N2;-N2} EGP/g, total: {totalPl:+N2;-N2} EGP";
                  }))
                : "No buy trades found.";

            var holdingsText = $"Gold balance: {goldBalance:F4}g\nCash balance: {cashBalance:N2} EGP";

            var system = "You are a financial assistant for Tibr. Analyze the user's portfolio "
                + "and answer their question. Be precise with numbers. Do not recommend actions unless asked.";

            var history = new List<Message>
            {
                new("user", $"Holdings:\n{holdingsText}\n\nTrades:\n{tradeText}\n\n"
                    + $"Price context:\n{priceCtx}\n\nQuestion: {userPrompt}")
            };

            var response = await _aiProvider.ChatAsync(system, history);
            return response.Content ?? "Sorry, I could not analyze your portfolio.";
        }

        public async Task<(string Reply, object? ToolCallRequest)> HandleAgenticAsync(string userPrompt, long userId, long conversationId)
        {
            var systemPrompt = "You are an order assistant for Tibr, a fractional gold investment app. "
                + "If the user wants to buy or sell gold or silver now, use the propose_order tool. "
                + "If the phrasing implies a conditional order (e.g., 'when price drops below', 'if it reaches'), "
                + "do NOT use propose_order — the conditional_order intent handles that separately.";

            var history = new List<Message> { new("user", userPrompt) };
            var tools = new List<object> { Tools.OrderBuilderTool.FunctionDeclaration };
            var response = await _aiProvider.ChatAsync(systemPrompt, history, tools);

            if (response.ToolCalls is { Count: > 0 })
            {
                var call = response.ToolCalls[0];
                return ("", call);
            }

            return (response.Content ?? "I can help you buy or sell gold and silver. What would you like to do?", null);
        }

        public async Task<(string Reply, object? ToolCallRequest)> HandleConditionalOrderAsync(string userPrompt, long userId)
        {
            var systemPrompt = "You are a strategy assistant for Tibr, a fractional gold investment app. "
                + "If the user wants to set a conditional order (buy/sell when price reaches a target), "
                + "use the create_strategy_order tool. Capture asset, side (buy/sell), operator (greater_than/less_than), "
                + "target_price_egp, execution_type (alert_only/auto_execute), quantity_grams, and expires_in_days.";

            var history = new List<Message> { new("user", userPrompt) };
            var tools = new List<object> { Tools.OrderBuilderTool.CreateStrategyFunctionDeclaration };
            var response = await _aiProvider.ChatAsync(systemPrompt, history, tools);

            if (response.ToolCalls is { Count: > 0 })
            {
                var call = response.ToolCalls[0];
                return ("", call);
            }

            return (response.Content ?? "I can help you set conditional orders for gold and silver. "
                + "For example: 'buy 10g of gold when price drops below 8000 EGP/g'.", null);
        }

        public async Task<string> HandlePlannerAsync(string userPrompt, long userId)
        {
            var goal = await _goalParser.ParseAsync(userPrompt);

            if (goal.ClarificationNeeded)
                return goal.ClarificationQuestion ?? "Could you provide more details about your savings goal?";

            var balanceResult = await _walletService.GetBalancesAsync(userId);
            var priceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);

            var currentGrams = balanceResult.IsSuccess
                ? (balanceResult.Data!.FirstOrDefault(b => b.WalletType == WalletType.Gold)
                    ?.AvailableBalance ?? 0)
                : 0;

            var currentPrice = priceResult.IsSuccess && priceResult.Data is not null
                ? priceResult.Data.SellPrice
                : 0;

            var mathResult = GoalMathService.Compute(goal, currentGrams, currentPrice);

            var goalTypeLabel = goal.GoalType switch
            {
                "reach_grams" => $"own {goal.TargetAmount:F2}g of {goal.Asset}",
                "reach_value_egp" => $"a portfolio worth {goal.TargetAmount:N0} EGP",
                "monthly_budget" => $"invest {goal.TargetAmount:N0} EGP/month",
                _ => "your goal"
            };

            var advisePrompt = $"You are a financial advisor for Tibr. "
                + $"The user wants to achieve {goalTypeLabel} within {goal.TimeframeWeeks} weeks.\n"
                + $"Current holdings: {currentGrams:F4}g {goal.Asset}\n"
                + $"Current price: {currentPrice:N2} EGP/g\n"
                + $"Required weekly investment: {mathResult.RequiredWeeklyEgp:N2} EGP\n"
                + $"Projected completion: {mathResult.ProjectedCompletionDate:yyyy-MM-dd}\n\n"
                + $"Provide encouraging, practical advice. Do NOT recompute any numbers yourself.";

            var history = new List<Message> { new("user", userPrompt) };
            var adviceResponse = await _aiProvider.ChatAsync(advisePrompt, history);
            return adviceResponse.Content ?? "Here's your savings plan. Check back as you make progress!";
        }
    }
}
