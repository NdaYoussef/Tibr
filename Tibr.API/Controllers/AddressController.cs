using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.AddressServices;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [HttpGet]
        public async Task<ActionResult<List<AddressDto>>> GetAll()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _addressService.GetByUserAsync(userId.Value);
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<ActionResult<AddressDto>> Create([FromBody] CreateAddressDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            dto.UserId = userId.Value;
            var result = await _addressService.CreateAsync(dto);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpDelete("{id:long}")]
        public async Task<ActionResult> Delete(long id)
        {
            var result = await _addressService.DeleteAsync(id);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
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
