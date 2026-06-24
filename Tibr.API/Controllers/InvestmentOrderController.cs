using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.InvestmentOrderServices;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/investment-orders")]
    [Authorize]
    public class InvestmentOrderController : ControllerBase
    {
        private readonly IInvestmentOrderService _orderService;

        public InvestmentOrderController(IInvestmentOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("strategy")]
        public async Task<ActionResult<InvestmentOrderDto>> CreateStrategy([FromBody] CreateStrategyOrderDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _orderService.CreateStrategyOrderAsync(userId.Value, dto);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpGet]
        public async Task<ActionResult<List<InvestmentOrderDto>>> GetAll()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _orderService.GetUserOrdersAsync(userId.Value);
            return Ok(result.Data);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<InvestmentOrderDto>> GetById(long id)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _orderService.GetOrderByIdAsync(userId.Value, id);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpDelete("{id:long}")]
        public async Task<ActionResult> Cancel(long id)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _orderService.CancelOrderAsync(userId.Value, id);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return NoContent();
        }

        private long? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim is null || !long.TryParse(claim.Value, out var userId))
                return null;
            return userId;
        }
    }
}
