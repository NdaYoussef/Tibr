using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.WalletServices;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet]
        public async Task<ActionResult<List<WalletBalanceDto>>> GetBalances()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _walletService.GetBalancesAsync(userId.Value);
            return Ok(result.Data);
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<List<WalletTransactionDto>>> GetTransactions()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _walletService.GetTransactionHistoryAsync(userId.Value);
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
