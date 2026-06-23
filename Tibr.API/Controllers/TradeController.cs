using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.TradeServices;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TradeController : ControllerBase
    {
        private readonly ITradeService _tradeService;

        public TradeController(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        [HttpPost("buy")]
        public async Task<ActionResult<InvestmentOrderDto>> Buy([FromBody] DirectBuyDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _tradeService.ExecuteDirectBuyAsync(userId.Value, dto);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpPost("sell")]
        public async Task<ActionResult<InvestmentOrderDto>> Sell([FromBody] DirectSellDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _tradeService.ExecuteDirectSellAsync(userId.Value, dto);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
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
