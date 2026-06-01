using MediatR;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;


namespace Tibr.Application.Services.Auth
{
    public record VerifyEmailCommand(VerifyOtpRequest Model) : IRequest<AuthResponse>;

    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, AuthResponse>
    {
        private readonly DbContext _context;
        public VerifyEmailCommandHandler(DbContext context) => _context = context;

        public async Task<AuthResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Email == request.Model.Email, cancellationToken);
            if (user == null) return new AuthResponse(false, "المستخدم غير موجود.", "User not found.");

            if (user.OtpCode != request.Model.Otp || user.OtpExpiry < DateTime.UtcNow)
                return new AuthResponse(false, "رمز التحقق غير صحيح أو انتهت صلاحيته.", "The verification code is incorrect or has expired.");

            user.OtpVerified = true;
            user.OtpCode = null;
            user.OtpExpiry = null;

            await _context.SaveChangesAsync(cancellationToken);
            return new AuthResponse(true, "تم تفعيل الحساب بنجاح، يمكنك رفع المستندات المطلوبة ليتم توثيق الحساب", "The account has been successfully activated. You can upload the required documents to complete account verification.", userId: user.Id.ToString());
        }
    }
}
