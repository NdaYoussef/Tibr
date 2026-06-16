using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.DeliveryServices;
using Tibr.Domain.Enums;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeliveryController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;

        public DeliveryController(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        [HttpPost]
        public async Task<ActionResult<DeliveryDto>> Create([FromBody] CreateDeliveryRequestDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _deliveryService.CreateRequestAsync(userId.Value, dto);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<DeliveryDto>> GetById(long id)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var result = await _deliveryService.GetByIdAsync(userId.Value, id);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpPatch("{id:long}/dispatch")]
        public async Task<ActionResult> Dispatch(long id, [FromBody] DispatchDto dto)
        {
            var result = await _deliveryService.ConfirmDispatchAsync(id, dto.TrackingNumber);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result);
        }

        [HttpPatch("{id:long}/status")]
        public async Task<ActionResult> UpdateStatus(long id, [FromBody] UpdateDeliveryStatusDto dto)
        {
            var result = await _deliveryService.UpdateStatusAsync(id, dto.Status);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result);
        }

        [HttpGet("my-deliveries")]
        public async Task<IActionResult> GetMyDeliveries()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            long userId = long.Parse(userIdClaim);

            var result = await _deliveryService.GetUserDeliveriesAsync(userId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        private long? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim is null || !long.TryParse(claim.Value, out var userId))
                return null;
            return userId;
        }
    }

    public class DispatchDto
    {
        public string TrackingNumber { get; set; } = string.Empty;
    }

    public class UpdateDeliveryStatusDto
    {
        public DeliveryStatus Status { get; set; }
    }
}
