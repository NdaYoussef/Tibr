using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private const long HardcodedUserId = 1; // Temporary user ID until auth integration

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var result = await _cartService.GetCartByUserIdAsync(HardcodedUserId);

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

            var result = await _cartService.AddToCartAsync(HardcodedUserId, dto);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }

        [HttpDelete("items/{cartItemId:long}")]
        public async Task<IActionResult> RemoveFromCart(long cartItemId)
        {
            var result = await _cartService.RemoveFromCartAsync(HardcodedUserId, cartItemId);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _cartService.ClearCartAsync(HardcodedUserId);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return NoContent();
        }
    }
}
