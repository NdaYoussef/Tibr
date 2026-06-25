using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Dtos.Payment;
using Tibr.Application.Services.DepositServices;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepositController : ControllerBase
    {
        private readonly IDepositService _depositService;

        public DepositController(IDepositService depositService)
        {
            _depositService = depositService;
        }

        [HttpPost("initiate")]
        public async Task<ActionResult<string>> Initiate([FromBody] InitiateDepositDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _depositService.InitiateAsync(userId.Value, dto);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(new { checkoutUrl = result.Data });
        }

        [HttpGet("verify/{depositId:long}")]
        public async Task<ActionResult<VerifyStatusResponse>> Verify(long depositId)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _depositService.VerifyDepositAsync(depositId);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<DepositDto>>> GetHistory()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _depositService.GetUserDepositsAsync(userId.Value);
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
