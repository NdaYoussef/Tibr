using MediatR;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Application.Interfaces;
using Tibr.Domain.Entities;

namespace Tibr.Application.Services.Auth
{
    public record ForgotPasswordCommand(ForgotPasswordRequestData Model) : IRequest<AuthResponse>;

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, AuthResponse>
    {
        private readonly DbContext _context;
        private readonly IEmailService _emailService; 

        public ForgotPasswordCommandHandler(DbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<AuthResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            // تنظيف الإيميل لضمان دقة البحث
            var inputEmail = request.Model.Email.Trim().ToLower();

            var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == inputEmail, cancellationToken);

            // حماية أمنية: إذا كان البريد غير موجود، نعيد رسالة نجاح موهمة لمنع الـ Hackers من فحص الإيميلات المسجلة
            if (user == null)
                return new AuthResponse(true, "إذا كان البريد مسجلاً، فقد أرسلنا رمز استعادة كلمة المرور.", "If the email is registered, we have sent a password recovery code.");

            var otp = new Random().Next(100000, 1000000).ToString(); // 👈 تعديل لتوليد 6 أرقام بدقة
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(15);

            await _context.SaveChangesAsync(cancellationToken);

            // ✉️ تمبلت فاخر بالـ Dark Mode الخاص بـ استعادة كلمة المرور لـ Aurum Vault
            string emailBody = $@"
<div style='background-color: #0d1117; padding: 40px 10px; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; direction: rtl; text-align: right;'>
    <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 550px; background-color: #161b22; border: 1px solid #30363d; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.5);'>
        <tr>
            <td align='center' style='padding: 30px 20px 20px 20px; border-bottom: 1px solid #21262d;'>
                <h1 style='color: #d4af37; font-size: 28px; font-weight: 700; margin: 0; letter-spacing: 1px;'>
                    AURUM VAULT
                </h1>
                <p style='color: #8b949e; font-size: 12px; text-transform: uppercase; margin: 5px 0 0 0; letter-spacing: 2px;'>
                    Reset Your Password
                </p>
            </td>
        </tr>
        
        <tr>
            <td style='padding: 40px 30px 30px 30px;'>
                <h2 style='color: #ffffff; font-size: 20px; font-weight: 600; margin-top: 0; margin-bottom: 16px;'>
                    طلب إعادة تعيين كلمة المرور
                </h2>
                <p style='color: #c9d1d9; font-size: 15px; line-height: 1.6; margin: 0;'>
                    مرحباً {user.FirstName}، لقد استقبلنا طلباً لإعادة تعيين كلمة المرور الخاصة بمحفظتك الاستثمارية. يرجى استخدام رمز الأمان المؤقت التالي لإتمام العملية:
                </p>
                
                <table border='0' cellpadding='0' cellspacing='0' width='100%' style='margin: 35px 0;'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #211a14 0%, #161b22 100%); border-right: 4px solid #e74c3c; border-radius: 6px; padding: 25px; text-align: center;'>
                            <span style='color: #8b949e; font-size: 13px; display: block; margin-bottom: 10px; font-weight: 550;'>
                                رمز الأمان المؤقت (Reset Code)
                            </span>
                            <span style='color: #e74c3c; font-size: 36px; font-weight: 700; letter-spacing: 6px; display: block; font-family: monospace, sans-serif;'>
                                {otp}
                            </span>
                        </td>
                    </tr>
                </table>
                
                <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                    <tr>
                        <td style='color: #8b949e; font-size: 13px; line-height: 1.5;'>
                            • هذا الرمز مخصص لحمايتك وصالح لمدة <strong>15 دقيقة</strong> فقط.<br>
                            • إذا لم تكن أنت من طلب إعادة التعيين، يرجى المسارعة بتأمين حسابك وتغيير بياناتك فوراً.
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        
        <tr>
            <td style='padding: 20px 30px; background-color: #0f141c; border-top: 1px solid #21262d; text-align: center;'>
                <p style='color: #484f58; font-size: 12px; margin: 0;'>
                    &copy; 2026 Aurum Vault. All rights reserved.
                </p>
            </td>
        </tr>
    </table>
</div>";

            await _emailService.SendEmailAsync(user.Email, "طلب استعادة كلمة المرور - Aurum Vault", emailBody);

            return new AuthResponse(true, "تم إرسال رمز استعادة كلمة المرور إلى بريدك الإلكتروني بنجاح.", "A password recovery code has been successfully sent to your email address.");
        }
    }
}