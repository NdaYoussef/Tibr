using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tibr.Application.Services.FavoriteServices;

namespace Tibr.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoriteController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }
        [HttpGet("check/{productId:long}")]
        public async Task<IActionResult> IsFavorite(long productId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized: Missing user ID." });
            }

            var result = await _favoriteService.IsFavoriteAsync(currentUserId, productId);

            return Ok(result);
        }

        [HttpPost("toggle/{productId:long}")]
        public async Task<IActionResult> ToggleFavorite(long productId)
        {
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"{claim.Type} = {claim.Value}");
            }
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized: Missing user ID." });
            }

            var result = await _favoriteService.ToggleFavoriteAsync(currentUserId, productId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("my-list")]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized: Missing user ID." });
            }

            var result = await _favoriteService.GetUserFavoritesAsync(currentUserId);
            return Ok(result);
        }
    }
}
