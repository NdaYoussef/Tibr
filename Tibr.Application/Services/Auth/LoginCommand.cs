using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Tibr.Application.Services.Auth
{
    public record LoginCommand(LoginRequestData Model) : IRequest<AuthResponse>;

    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly DbContext _context;
        private readonly IConfiguration _configuration;

        public LoginCommandHandler(DbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Email == request.Model.Email, cancellationToken);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Model.Password, user.Password))
                return new AuthResponse(false, "بيانات الدخول غير صحيحة.", "The login details are incorrect.");

            if (!user.OtpVerified)
                return new AuthResponse(false, "يرجى تفعيل الحساب أولاً عبر رمز الـ OTP.", "Please activate your account first via OTP code.");

            if (user.KycStatus != "Verified")
                return new AuthResponse(false, "لم يتم توثيق حساب بعد", "No account verified yet");


            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddDays(request.Model.RememberMe ? 30 : 1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new AuthResponse(
                true,
                "تم تسجيل الدخول بنجاح.",
                "Login successful.",
                new JwtSecurityTokenHandler().WriteToken(token),
                token.ValidTo,
                user.Id.ToString()
            );
        }
    }
}
