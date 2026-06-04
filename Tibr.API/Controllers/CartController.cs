using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Tibr.Application.Dtos;
using Tibr.Application.Services.CartServices;
using Tibr.Application.Services.CategoryServices;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {

        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized: Missing user ID." });
            }

            var result = await _cartService.GetCartByUserIdAsync(currentUserId);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { error = "Invalid cart request data." });
            }
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized: Missing user ID." });
            }

            var result = await _cartService.AddToCartAsync(currentUserId, dto);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }

        [HttpDelete("items/{cartItemId:long}")]
        public async Task<IActionResult> RemoveFromCart(long cartItemId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized: Missing user ID." });
            }
            var result = await _cartService.RemoveFromCartAsync(currentUserId, cartItemId);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized: Missing user ID." });
            }
            var result = await _cartService.ClearCartAsync(currentUserId);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return NoContent();
        }
    }
}
