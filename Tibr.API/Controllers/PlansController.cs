using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.PlanServices;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/plans")]
    [Authorize]
    public class PlansController : ControllerBase
    {
        private readonly IPlanService _planService;

        public PlansController(IPlanService planService)
        {
            _planService = planService;
        }

        [HttpGet("active")]
        public async Task<ActionResult<PlanDto>> GetActive()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _planService.GetActivePlanAsync(userId.Value);
            if (result.IsFailure)
                return NotFound(new { message = result.ErrorMessage });

            return Ok(result.Data);
        }

        [HttpPost("reevaluate")]
        public async Task<ActionResult<ReevaluatePlanResultDto>> Reevaluate()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _planService.ReevaluateAsync(userId.Value);
            if (result.IsFailure)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Data);
        }

        [HttpPut("extend-deadline")]
        public async Task<ActionResult<PlanDto>> ExtendDeadline([FromBody] ExtendDeadlineDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            if (dto.NewTimeframeWeeks <= 0)
                return BadRequest(new { message = "New timeframe must be positive." });

            var result = await _planService.ExtendDeadlineAsync(userId.Value, dto.NewTimeframeWeeks);
            if (result.IsFailure)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Data);
        }

        [HttpPut("cancel")]
        public async Task<ActionResult<PlanDto>> Cancel()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _planService.CancelPlanAsync(userId.Value);
            if (result.IsFailure)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Data);
        }

        private long? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is null || !long.TryParse(claim.Value, out var userId) ? null : userId;
        }
    }
}
