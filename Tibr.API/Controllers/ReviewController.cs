using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.ReviewServices;

namespace Tibr.API.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _reviewService.CreateAsync(dto, userId.Value);
        if (result.IsFailure)
            return BadRequest(result.ErrorMessage);

        return StatusCode(201);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Update(long id, [FromBody] UpdateReviewDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _reviewService.UpdateAsync(id, dto, userId.Value);
        if (result.IsFailure)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<List<ReviewDto>>> GetMyReviews()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _reviewService.GetByUserIdAsync(userId.Value);
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
