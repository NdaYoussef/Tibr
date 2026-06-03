using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using MediatR;
using Tibr.Application.Services.Auth;
using Tibr.Application.Services.Kyc;

namespace Tibr.API.Controllers
{
    [ApiController]
    //[Route("api/[controller]")]
    [Route("api")]

    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator) => _mediator = mediator;

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
    }
}
