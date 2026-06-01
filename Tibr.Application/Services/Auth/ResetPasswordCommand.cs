using MediatR;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Services.Auth
{
    public record ResetPasswordCommand(ResetPasswordRequestData Model) : IRequest<AuthResponse>;

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, AuthResponse>
    {
        private readonly DbContext _context;
        public ResetPasswordCommandHandler(DbContext context) => _context = context;

        public async Task<AuthResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (request.Model.NewPassword != request.Model.ConfirmPassword)
                return new AuthResponse(false, "كلمة المرور وتأكيدها غير متطابقين.", "The password and its confirmation do not match.");

            var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Email == request.Model.Email, cancellationToken);
            if (user == null) return new AuthResponse(false, "حدث خطأ ما.", "Something went wrong.");

            if (user.OtpCode != request.Model.Otp || user.OtpExpiry < DateTime.UtcNow)
                return new AuthResponse(false, "رمز التحقق غير صحيح أو انتهت صلاحيته.", "The verification code is incorrect or has expired.");

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Model.NewPassword);
            user.OtpCode = null;
            user.OtpExpiry = null;

            await _context.SaveChangesAsync(cancellationToken);
            return new AuthResponse(true, "تم إعادة تعيين كلمة المرور بنجاح، يمكنك تسجيل الدخول الآن.", "Your password has been successfully reset, you can now log in.");
        }
    }
}
