using MediatR;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Application.Interfaces;

namespace Tibr.Application.Services.Auth
{
    public record RegisterCommand(RegisterRequestData Model) : IRequest<AuthResponse>;
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
    {
        private readonly DbContext _context;
        private readonly IEmailService _emailService;
        public RegisterCommandHandler(DbContext context, IEmailService emailService) {
            _context = context;
            _emailService = emailService;
        }

        public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (request.Model.Password != request.Model.ConfirmPassword)
                return new AuthResponse(false, "كلمة المرور وتأكيدها غير متطابقين.", "The password and its confirmation do not match.");

            var userExists = await _context.Set<User>().AnyAsync(u => u.Email == request.Model.Email, cancellationToken);
            if (userExists)
                return new AuthResponse(false, "البريد الإلكتروني مسجل بالفعل.","The email address is already registered.");

            var otp = new Random().Next(100000, 999999).ToString();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Model.Password);

            var user = new User
            {
                FirstName = request.Model.FirstName,
                LastName = request.Model.LastName,
                Email = request.Model.Email,
                Phone = request.Model.Phone,
                Password = hashedPassword,
                Status = "Active",
                OtpVerified = false,
                KycStatus = "None",
                OtpCode = otp,
                OtpExpiry = DateTime.UtcNow.AddMinutes(300)
            };

            await _context.Set<User>().AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            string emailBody = $@"
<div style='background-color: #0d1117; padding: 40px 10px; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; direction: rtl; text-align: right;'>
    <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 550px; background-color: #161b22; border: 1px solid #30363d; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.5);'>
        <tr>
            <td align='center' style='padding: 30px 20px 20px 20px; border-bottom: 1px solid #21262d;'>
                <h1 style='color: #d4af37; font-size: 28px; font-weight: 700; margin: 0; letter-spacing: 1px; text-shadow: 0 2px 4px rgba(0,0,0,0.3);'>
                    AURUM VAULT
                </h1>
                <p style='color: #8b949e; font-size: 12px; text-transform: uppercase; margin: 5px 0 0 0; letter-spacing: 2px;'>
                    Secure Digital Investment
                </p>
            </td>
        </tr>
        
        <tr>
            <td style='padding: 40px 30px 30px 30px;'>
                <h2 style='color: #ffffff; font-size: 20px; font-weight: 600; margin-top: 0; margin-bottom: 16px;'>
                    مرحباً بك في مجتمع المستثمرين
                </h2>
                <p style='color: #c9d1d9; font-size: 15px; line-height: 1.6; margin: 0;'>
                    شكراً لتسجيلك في منصة <strong>Aurum Vault</strong>. تتبقى خطوة واحدة لتأمين حسابك وتفعيل محفظتك الاستثمارية للبدء في تداول الأصول الرقمية بأمان.
                </p>
                
                <table border='0' cellpadding='0' cellspacing='0' width='100%' style='margin: 35px 0;'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #1f242c 0%, #161b22 100%); border-right: 4px solid #d4af37; border-radius: 6px; padding: 25px; text-align: center;'>
                            <span style='color: #8b949e; font-size: 13px; display: block; margin-bottom: 10px; font-weight: 550;'>
                                رمز التحقق المؤقت (OTP)
                            </span>
                            <span style='color: #d4af37; font-size: 36px; font-weight: 700; letter-spacing: 6px; display: block; font-family: monospace, sans-serif;'>
                                {otp}
                            </span>
                        </td>
                    </tr>
                </table>
                
                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                    <tr>
                        <td style='color: #8b949e; font-size: 13px; line-height: 1.5;'>
                            • هذا الرمز صالح للاستخدام لمرة واحدة فقط وينتهي بعد <strong>15 دقيقة</strong>.<br>
                            • لا تشارك هذا الرمز مع أي شخص مطلقاً، فريق عمل المنصة لن يطلب منك هذا الكود.
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        
        <tr>
            <td style='padding: 20px 30px; background-color: #0f141c; border-top: 1px solid #21262d; text-align: center;'>
                <p style='color: #484f58; font-size: 12px; margin: 0 0 10px 0;'>
                    إذا لم تقم بإنشاء حساب في Aurum Vault، يمكنك تجاهل هذا البريد الإلكتروني بأمان.
                </p>
                <p style='color: #8b949e; font-size: 11px; margin: 0;'>
                    &copy; 2026 Aurum Vault. All rights reserved.
                </p>
            </td>
        </tr>
    </table>
</div>";

            await _emailService.SendEmailAsync(user.Email, "تأكيد بريدك الإلكتروني - Aurum Vault", emailBody);

            return new AuthResponse(true, "تم إنشاء الحساب بنجاح. يرجى مراجعة بريدك الإلكتروني لتأكيد رمز الـ OTP.","Account created successfully. Please check your email to confirm your OTP code.");
        }
    }
}
