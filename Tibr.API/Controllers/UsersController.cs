using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tibr.Application.Dtos;
using Tibr.Application.Services.UserServices;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserListItemDto>>> GetAll(
            [FromQuery] string? searchQuery,
            [FromQuery] string? statusFilter,
            [FromQuery] string? kycStatusFilter)
        {
            var result = await _userService.GetUsersAsync(searchQuery, statusFilter, kycStatusFilter);
            if (result.IsFailure)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<UserDetailsDto>> GetById(long id)
        {
            var result = await _userService.GetByIdAsync(id);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<UserDetailsDto>> Update(long id, [FromBody] UpdateUserDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Mismatched user ID.");

            var result = await _userService.UpdateAsync(id, dto);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpPost("{id:long}/toggle-status")]
        public async Task<ActionResult<string>> ToggleStatus(long id)
        {
            var result = await _userService.ToggleStatusAsync(id);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpPost("{id:long}/kyc-status")]
        public async Task<ActionResult<string>> UpdateKycStatus(long id, [FromBody] UpdateUserKycStatusDto dto)
        {
            var result = await _userService.UpdateKycStatusAsync(id, dto.Status);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpDelete("{id:long}")]
        public async Task<ActionResult> Delete(long id)
        {
            var result = await _userService.DeleteAsync(id);
            if (result.IsFailure)
                return NotFound(result.ErrorMessage);
            return NoContent();
        }
    }
}
