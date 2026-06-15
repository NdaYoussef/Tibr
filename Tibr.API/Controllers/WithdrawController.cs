using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.WithdrawServices;

namespace Tibr.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WithdrawController : ControllerBase
{
    private readonly IWithdrawService _withdrawService;

    public WithdrawController(IWithdrawService withdrawService)
    {
        _withdrawService = withdrawService;
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateWithdrawDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _withdrawService.CreateAsync(dto, userId.Value);
        if (result.IsFailure)
            return BadRequest(result.ErrorMessage);

        return StatusCode(201);
    }

    private long? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null || !long.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }
}
