using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tibr.Application.Dtos.TicketDtos;
using Tibr.Application.Services.TicketServices;

namespace Tibr.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketController(ITicketService _ticketService) : ControllerBase
    {
        [HttpPost("admin-reply")]
        // [Authorize(Roles = "Admin")] // يفضل حمايتها إذا كنت بتستخدم Authentication
        public async Task<IActionResult> AdminReply([FromBody] CreateTicketDto dto)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(adminIdClaim) || !long.TryParse(adminIdClaim, out long currentAdminId))
            {
                return Unauthorized(new { message = "Unauthorized: Missing Admin ID." });
            }

            var result = await _ticketService.ReplyToTicketAsync(dto, adminId: currentAdminId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("message/{id:long}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMessage(long id)
        {
            var result = await _ticketService.DeleteMessageAsync(id);

            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }
    }
}
