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
    public record AdminLoginCommand(LoginRequestData Model) : IRequest<AuthResponse>;

    public class AdminLoginCommandHandler : IRequestHandler<AdminLoginCommand, AuthResponse>
    {
        private readonly DbContext _context;
        private readonly IConfiguration _configuration;

        public AdminLoginCommandHandler(DbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
        {
            // Check if the user is an admin
            var admin = await _context.Set<Domain.Entities.Admin>().FirstOrDefaultAsync(a => a.Email == request.Model.Email, cancellationToken);

            if (admin == null || admin.Status != "Active")
                return new AuthResponse(false, "The login details are incorrect or account is inactive.", "The login details are incorrect or account is inactive.");

           
            var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Email == request.Model.Email, cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Model.Password, user.Password))
                return new AuthResponse(false, "The login details are incorrect.", "The login details are incorrect.");

            if (!user.OtpVerified)
                return new AuthResponse(false, "Please activate your account first via OTP code.", "Please activate your account first via OTP code.");

            // Create admin JWT token
            var authClaims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(ClaimTypes.Name, admin.Name),
                new System.Security.Claims.Claim(ClaimTypes.Role, "Admin")
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
                "Admin login successful.",
                "Admin login successful.",
                new JwtSecurityTokenHandler().WriteToken(token),
                token.ValidTo,
                user.Id.ToString()
            );
        }
    }
}
