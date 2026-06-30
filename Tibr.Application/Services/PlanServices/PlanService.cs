using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Application.Dtos.ChatDtos;
using Tibr.Application.Services.AssetPriceServices;
using Tibr.Application.Services.WalletServices;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;
using Tibr.Application.Services.AiChatServices;

namespace Tibr.Application.Services.PlanServices
{
    public class PlanService : IPlanService
    {
        private readonly IGenericRepository<Plan, long> _planRepo;
        private readonly IGenericRepository<Trade, long> _tradeRepo;
        private readonly IWalletService _walletService;
        private readonly IAssetPriceService _priceService;

        public PlanService(
            IGenericRepository<Plan, long> planRepo,
            IGenericRepository<Trade, long> tradeRepo,
            IWalletService walletService,
            IAssetPriceService priceService)
        {
            _planRepo = planRepo;
            _tradeRepo = tradeRepo;
            _walletService = walletService;
            _priceService = priceService;
        }

        public async Task<Result<PlanDto>> CreateFromGoalAsync(
            long userId, GoalParseResult goal, decimal priceAtCreation, decimal? silverPriceAtCreation, string language = "en")
        {
            var existing = await _planRepo.GetAll(p => p.UserId == userId && p.Status == PlanStatus.Active)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return Result<PlanDto>.Failure(language == "ar"
                    ? "لديك بالفعل خطة نشطة. أكملها أو ألغها أولاً."
                    : "You already have an active plan. Complete or cancel it first.");

            if (goal.Asset == "both" && goal.GoalType != "reach_value_egp")
                return Result<PlanDto>.Failure(language == "ar"
                    ? "الأصل \"كلاهما\" يدعم فقط نوع الهدف reach_value_egp."
                    : "\"both\" asset only supports reach_value_egp goal type.");

            if (goal.GoalType != "monthly_budget" && goal.TimeframeWeeks <= 0)
                return Result<PlanDto>.Failure(language == "ar"
                    ? "المدة الزمنية مطلوبة لهذا النوع من الأهداف."
                    : "Timeframe is required for this goal type.");

            // Skip creation if user already meets the goal
            if (goal.GoalType == "reach_grams")
            {
                var walletType = goal.Asset == "silver" ? WalletType.Silver : WalletType.Gold;
                var balanceResult = await _walletService.GetAvailableBalanceAsync(userId, walletType);
                if (balanceResult.IsSuccess && balanceResult.Data >= goal.TargetAmount)
                {
                    var metal = goal.Asset == "silver" ? "silver" : "gold";
                    var metalAr = metal == "silver" ? "الفضة" : "الذهب";
                    return Result<PlanDto>.Failure(language == "ar"
                        ? $"أنت تملك بالفعل {balanceResult.Data:F4}g من {metalAr}، وهو ما يتجاوز هدفك البالغ {goal.TargetAmount:F4}g. لا حاجة لخطة!"
                        : $"You already own {balanceResult.Data:F4}g of {metal}, which exceeds your target of {goal.TargetAmount:F4}g. No plan needed!");
                }
            }
            else if (goal.GoalType == "reach_value_egp")
            {
                if (goal.Asset == "both")
                {
                    var goldBalance = await _walletService.GetAvailableBalanceAsync(userId, WalletType.Gold);
                    var silverBalance = await _walletService.GetAvailableBalanceAsync(userId, WalletType.Silver);
                    var goldVal = (goldBalance.IsSuccess ? goldBalance.Data : 0) * priceAtCreation;
                    var silverVal = (silverBalance.IsSuccess ? silverBalance.Data : 0) * (silverPriceAtCreation ?? 0);
                    var total = goldVal + silverVal;
                    if (total >= goal.TargetAmount)
                        return Result<PlanDto>.Failure(language == "ar"
                            ? $"محفظتك من الذهب والفضة قيمتها {total:N2} جنيهاً، وهو ما يحقق هدفك البالغ {goal.TargetAmount:N2} جنيهاً. لا حاجة لخطة!"
                            : $"Your gold + silver portfolio is worth {total:N2} EGP, which already meets your target of {goal.TargetAmount:N2} EGP. No plan needed!");
                }
                else
                {
                    var walletType = goal.Asset == "silver" ? WalletType.Silver : WalletType.Gold;
                    var balanceResult = await _walletService.GetAvailableBalanceAsync(userId, walletType);
                    if (balanceResult.IsSuccess)
                    {
                        var currentValue = balanceResult.Data * priceAtCreation;
                        if (currentValue >= goal.TargetAmount)
                        {
                            var metal = goal.Asset == "silver" ? "silver" : "gold";
                            var metalAr = metal == "silver" ? "الفضة" : "الذهب";
                            return Result<PlanDto>.Failure(language == "ar"
                                ? $"ممتلكاتك من {metalAr} بقيمة {currentValue:N2} جنيهاً تفي بالفعل بهدف {goal.TargetAmount:N2} جنيهاً. لا حاجة لخطة!"
                                : $"Your {metal} holdings are worth {currentValue:N2} EGP, which already meets your target of {goal.TargetAmount:N2} EGP. No plan needed!");
                        }
                    }
                }
            }

            var plan = new Plan
            {
                UserId = userId,
                GoalType = goal.GoalType ?? "reach_grams",
                Asset = goal.Asset ?? "gold",
                TargetAmount = goal.TargetAmount,
                TimeframeWeeks = goal.GoalType == "monthly_budget" ? null : goal.TimeframeWeeks,
                MonthlyBudgetEgp = goal.GoalType == "monthly_budget" ? goal.TargetAmount : null,
                PriceAtCreation = priceAtCreation,
                SilverPriceAtCreation = goal.Asset == "both" ? silverPriceAtCreation : null,
                Status = PlanStatus.Active,
            };

            await _planRepo.AddAsync(plan);
            await _planRepo.SaveChangesAsync();

            return Result<PlanDto>.Success(MapToDto(plan));
        }

        public async Task<Result<PlanDto>> GetActivePlanAsync(long userId)
        {
            var plan = await _planRepo.GetAll(p => p.UserId == userId && p.Status == PlanStatus.Active)
                .FirstOrDefaultAsync();

            if (plan is null)
                return Result<PlanDto>.Failure("You don't have an active plan.");

            return Result<PlanDto>.Success(MapToDto(plan));
        }

        public async Task<Result<ReevaluatePlanResultDto>> ReevaluateAsync(long userId, string language = "en")
        {
            var plan = await _planRepo.GetAll(p => p.UserId == userId && p.Status == PlanStatus.Active)
                .FirstOrDefaultAsync();

            if (plan is null)
                return Result<ReevaluatePlanResultDto>.Failure("You don't have an active plan.");
            if (plan.Status == PlanStatus.Expired)
                return Result<ReevaluatePlanResultDto>.Failure("Your plan has expired. Extend the deadline first.");
            if (plan.Status == PlanStatus.Completed)
                return Result<ReevaluatePlanResultDto>.Failure("Your plan is already completed!");
            if (plan.Status == PlanStatus.Cancelled)
                return Result<ReevaluatePlanResultDto>.Failure("Your plan was cancelled. Create a new one.");

            var weeksSinceCreation = (DateTime.UtcNow - plan.CreatedAt).TotalDays / 7.0;
            var result = new ReevaluatePlanResultDto();

            // Fetch live data — branch by asset type
            decimal goldPrice = 0, silverPrice = 0;
            decimal goldGrams = 0, silverGrams = 0;
            decimal? currentPrice = null;
            decimal? walletBalance = null;

            if (plan.Asset == "both")
            {
                var goldPriceResult = await _priceService.GetCurrentPriceAsync(AssetType.Gold);
                var silverPriceResult = await _priceService.GetCurrentPriceAsync(AssetType.Silver);
                goldPrice = goldPriceResult.IsSuccess && goldPriceResult.Data is not null ? goldPriceResult.Data.SellPrice : 0;
                silverPrice = silverPriceResult.IsSuccess && silverPriceResult.Data is not null ? silverPriceResult.Data.SellPrice : 0;

                var balances = await _walletService.GetBalancesAsync(userId);
                if (balances.IsSuccess && balances.Data is not null)
                {
                    goldGrams = balances.Data.FirstOrDefault(b => b.WalletType == WalletType.Gold)?.AvailableBalance ?? 0;
                    silverGrams = balances.Data.FirstOrDefault(b => b.WalletType == WalletType.Silver)?.AvailableBalance ?? 0;
                }

                result.CurrentPrice = goldPrice;
                result.SilverCurrentPrice = silverPrice;
                result.WalletBalance = goldGrams;
                result.SilverWalletBalance = silverGrams;
            }
            else
            {
                var assetType = plan.Asset == "silver" ? AssetType.Silver : AssetType.Gold;
                var walletType = plan.Asset == "silver" ? WalletType.Silver : WalletType.Gold;

                var priceResult = await _priceService.GetCurrentPriceAsync(assetType);
                currentPrice = priceResult.IsSuccess && priceResult.Data is not null ? priceResult.Data.SellPrice : 0;

                var balanceResult = await _walletService.GetAvailableBalanceAsync(userId, walletType);
                walletBalance = balanceResult.IsSuccess ? balanceResult.Data : 0;

                result.CurrentPrice = currentPrice;
                result.WalletBalance = walletBalance ?? 0;
            }

            // Check timeframe expired — skip for monthly_budget
            int? remainingWeeks = null;
            if (plan.TimeframeWeeks.HasValue)
            {
                remainingWeeks = (int)Math.Floor(plan.TimeframeWeeks.Value - weeksSinceCreation);
                if (remainingWeeks <= 0)
                {
                    plan.Status = PlanStatus.Expired;
                    await _planRepo.SaveChangesAsync();

                    result.Plan = MapToDto(plan);
                    result.Message = SystemMessages.PlanExpired(language);
                    return Result<ReevaluatePlanResultDto>.Success(result);
                }
                result.RemainingWeeks = remainingWeeks;
            }

            // Compute progress — branch by GoalType
            decimal progressPercent = 0;
            decimal? requiredWeeklyEgp = null;

            switch (plan.GoalType)
            {
                case "reach_grams":
                {
                    var bal = plan.Asset == "both"
                        ? goldGrams + silverGrams
                        : walletBalance ?? 0;
                    var remainingGrams = plan.TargetAmount - bal;
                    progressPercent = plan.TargetAmount > 0 ? bal / plan.TargetAmount * 100 : 0;

                    if (remainingGrams <= 0)
                    {
                        SetPriceMovement(result, plan, goldPrice, silverPrice, currentPrice);
                        plan.Status = PlanStatus.Completed;
                        await _planRepo.SaveChangesAsync();
                        result.Completed = true;
                        result.Message = SystemMessages.PlanGoalReachedGrams(language);
                        result.ProgressPercent = 100;
                        result.Plan = MapToDto(plan);
                        return Result<ReevaluatePlanResultDto>.Success(result);
                    }

                    var price = currentPrice ?? 0;
                    requiredWeeklyEgp = remainingWeeks.HasValue && remainingWeeks > 0
                        ? (remainingGrams * price) / remainingWeeks
                        : null;

                    var goalLabel = plan.Asset == "both" ? "gold + silver" : plan.Asset;
                    result.Message = $"You own {bal:F4}g of {plan.TargetAmount:F4}g {goalLabel} target ({progressPercent:F1}%). "
                        + $"At today's price{(plan.Asset == "both" ? "s" : "")}, you need {requiredWeeklyEgp:N2} EGP/week for the remaining {remainingWeeks} weeks.";
                    break;
                }

                case "reach_value_egp":
                {
                    decimal currentPortfolioValue;
                    if (plan.Asset == "both")
                    {
                        currentPortfolioValue = goldGrams * goldPrice + silverGrams * silverPrice;
                    }
                    else
                    {
                        currentPortfolioValue = (walletBalance ?? 0) * (currentPrice ?? 0);
                    }

                    progressPercent = plan.TargetAmount > 0 ? currentPortfolioValue / plan.TargetAmount * 100 : 0;
                    var remainingValue = plan.TargetAmount - currentPortfolioValue;

                    if (remainingValue <= 0)
                    {
                        SetPriceMovement(result, plan, goldPrice, silverPrice, currentPrice);
                        plan.Status = PlanStatus.Completed;
                        await _planRepo.SaveChangesAsync();
                        result.Completed = true;
                        result.Message = SystemMessages.PlanGoalReachedValue(language);
                        result.ProgressPercent = 100;
                        result.Plan = MapToDto(plan);
                        return Result<ReevaluatePlanResultDto>.Success(result);
                    }

                    requiredWeeklyEgp = remainingWeeks.HasValue && remainingWeeks > 0
                        ? remainingValue / remainingWeeks
                        : null;

                    result.Message = $"Your portfolio is worth {currentPortfolioValue:N2} EGP of {plan.TargetAmount:N2} EGP target ({progressPercent:F1}%). "
                        + $"You need {requiredWeeklyEgp:N2} EGP/week for the remaining {remainingWeeks} weeks.";
                    break;
                }

                case "monthly_budget":
                {
                    decimal investedEgp;
                    if (plan.Asset == "both")
                    {
                        // "both" + monthly_budget is rejected at creation — this branch is unreachable in v1,
                        // kept for future reference
                        investedEgp = await _tradeRepo
                            .GetAll(t => t.UserId == userId
                                      && (t.AssetType == AssetType.Gold || t.AssetType == AssetType.Silver)
                                      && t.Side == TradeSide.Buy
                                      && t.ExecutedAt >= plan.CreatedAt)
                            .SumAsync(t => t.TotalAmount);
                    }
                    else
                    {
                        var assetType = plan.Asset == "silver" ? AssetType.Silver : AssetType.Gold;
                        investedEgp = await _tradeRepo
                            .GetAll(t => t.UserId == userId
                                      && t.AssetType == assetType
                                      && t.Side == TradeSide.Buy
                                      && t.ExecutedAt >= plan.CreatedAt)
                            .SumAsync(t => t.TotalAmount);
                    }

                    result.InvestedSinceStart = investedEgp;
                    var budget = plan.MonthlyBudgetEgp ?? plan.TargetAmount;
                    var expectedByNow = budget * (decimal)(weeksSinceCreation / 4.33);
                    var onTrack = investedEgp >= expectedByNow * 0.8m;
                    progressPercent = expectedByNow > 0 ? investedEgp / expectedByNow * 100 : 0;

                    result.Message = $"Since your plan started ({weeksSinceCreation:F0} weeks ago), you've invested {investedEgp:N2} EGP. "
                        + $"At your planned budget of {budget:N2} EGP/month, you're {(onTrack ? "on track" : "off track")}.";
                    break;
                }
            }

            result.ProgressPercent = progressPercent;
            result.RequiredWeeklyEgp = requiredWeeklyEgp;

            SetPriceMovement(result, plan, goldPrice, silverPrice, currentPrice);

            plan.LastReevaluatedAt = DateTime.UtcNow;
            await _planRepo.SaveChangesAsync();

            result.Plan = MapToDto(plan);
            return Result<ReevaluatePlanResultDto>.Success(result);
        }

        public async Task<Result<PlanDto>> ExtendDeadlineAsync(long userId, int newTimeframeWeeks)
        {
            var plan = await _planRepo.GetAll(p => p.UserId == userId && p.Status == PlanStatus.Active)
                .FirstOrDefaultAsync();

            if (plan is null)
                return Result<PlanDto>.Failure("You don't have an active plan.");

            if (plan.GoalType == "monthly_budget")
                return Result<PlanDto>.Failure("Monthly budget plans don't have a deadline.");

            plan.TimeframeWeeks = newTimeframeWeeks;
            plan.Status = PlanStatus.Active;
            await _planRepo.SaveChangesAsync();

            return Result<PlanDto>.Success(MapToDto(plan));
        }

        public async Task<Result<PlanDto>> CancelPlanAsync(long userId)
        {
            var plan = await _planRepo.GetAll(p => p.UserId == userId && p.Status == PlanStatus.Active)
                .FirstOrDefaultAsync();

            if (plan is null)
                return Result<PlanDto>.Failure("You don't have an active plan.");

            plan.Status = PlanStatus.Cancelled;
            await _planRepo.SaveChangesAsync();

            return Result<PlanDto>.Success(MapToDto(plan));
        }

        private static void SetPriceMovement(ReevaluatePlanResultDto result, Plan plan,
            decimal goldPrice, decimal silverPrice, decimal? currentPrice)
        {
            if (plan.Asset == "both")
            {
                result.PriceChangePercent = goldPrice > 0 && plan.PriceAtCreation > 0
                    ? (goldPrice - plan.PriceAtCreation) / plan.PriceAtCreation * 100
                    : null;
                result.SilverPriceChangePercent = silverPrice > 0 && plan.SilverPriceAtCreation.HasValue && plan.SilverPriceAtCreation.Value > 0
                    ? (silverPrice - plan.SilverPriceAtCreation.Value) / plan.SilverPriceAtCreation.Value * 100
                    : null;
            }
            else
            {
                var price = currentPrice ?? 0;
                result.PriceChangePercent = price > 0 && plan.PriceAtCreation > 0
                    ? (price - plan.PriceAtCreation) / plan.PriceAtCreation * 100
                    : null;
            }
        }

        private static PlanDto MapToDto(Plan plan)
        {
            return new PlanDto
            {
                Id = plan.Id,
                GoalType = plan.GoalType,
                Asset = plan.Asset,
                TargetAmount = plan.TargetAmount,
                TimeframeWeeks = plan.TimeframeWeeks,
                MonthlyBudgetEgp = plan.MonthlyBudgetEgp,
                PriceAtCreation = plan.PriceAtCreation,
                SilverPriceAtCreation = plan.SilverPriceAtCreation,
                Status = plan.Status.ToString(),
                CreatedAt = plan.CreatedAt,
                LastReevaluatedAt = plan.LastReevaluatedAt,
            };
        }
    }
}
