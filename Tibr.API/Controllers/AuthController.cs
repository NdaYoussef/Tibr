using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using MediatR;
using Tibr.Application.Services.Auth;
using Tibr.Application.Services.Kyc;

namespace Tibr.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator) => _mediator = mediator;

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();
            var result = await _mediator.Send(new GetProfileQuery(userId.Value));
            return Ok(result);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();
            var result = await _mediator.Send(new UpdateProfileCommand(userId.Value, dto));
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();
            var result = await _mediator.Send(new ChangePasswordCommand(userId.Value, dto));
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestData model)
        {
            var result = await _mediator.Send(new RegisterCommand(model));
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest model)
        {
            var result = await _mediator.Send(new VerifyEmailCommand(model));
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult>  Login([FromBody] LoginRequestData model)
        {
            var result = await _mediator.Send(new LoginCommand(model));
            if (!result.IsSuccess) return Unauthorized(result);
            return Ok(result);
        }

        [HttpPost("admin-login")]
        [AllowAnonymous]
        public async Task<IActionResult> AdminLogin([FromBody] LoginRequestData model)
        {
            var result = await _mediator.Send(new AdminLoginCommand(model));
            if (!result.IsSuccess) return Unauthorized(result);
            return Ok(result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestData model)
        {
            var result = await _mediator.Send(new ForgotPasswordCommand(model));
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestData model)
        {
            var result = await _mediator.Send(new ResetPasswordCommand(model));
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("submit-kyc")]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitKyc(
            [FromForm] long userId,
            [FromForm] string documentType,
            [FromForm] string documentNumber,
            IFormFile documentFront,
            IFormFile documentBack,
            IFormFile selfieImage)
        {
            var command = new SubmitKycCommand(userId, documentType, documentNumber, documentFront, documentBack, selfieImage);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess) return BadRequest(result);
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
}
