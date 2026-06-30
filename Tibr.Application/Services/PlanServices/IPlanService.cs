using Tibr.Application.Dtos;
using Tibr.Application.Dtos.ChatDtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.PlanServices
{
    public interface IPlanService
    {
        Task<Result<PlanDto>> CreateFromGoalAsync(long userId, GoalParseResult goal, decimal priceAtCreation, decimal? silverPriceAtCreation, string language = "en");
        Task<Result<PlanDto>> GetActivePlanAsync(long userId);
        Task<Result<ReevaluatePlanResultDto>> ReevaluateAsync(long userId, string language = "en");
        Task<Result<PlanDto>> ExtendDeadlineAsync(long userId, int newTimeframeWeeks);
        Task<Result<PlanDto>> CancelPlanAsync(long userId);
    }
}
