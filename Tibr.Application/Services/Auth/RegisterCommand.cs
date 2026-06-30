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
                return new AuthResponse(false, "البريد الإلكتروني مسجل بالفعل.", "The email address is already registered.");

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
                OtpExpiry = DateTime.UtcNow.AddMinutes(15)
            };

            await _context.Set<User>().AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // تصفية النصوص بناءً على لغة الطلب داخل الـ C# مباشرة
            bool isEn = request.Model.lang == "en";

            string dir = isEn ? "ltr" : "rtl";
            string textAlign = isEn ? "left" : "right";
            string borderRight = isEn ? "none" : "4px solid #d4af37";
            string borderLeft = isEn ? "4px solid #d4af37" : "none";

            string welcomeMsg = isEn ? "Welcome to the Investors Community" : "مرحباً بك في مجتمع المستثمرين";
            string descriptionMsg = isEn
                ? "Thank you for registering with the <strong>Tibr</strong> platform. One more step remains to secure your account and activate your investment portfolio to begin trading digital assets safely."
                : "شكراً لتسجيلك في منصة <strong>Tibr</strong>. تتبقى خطوة واحدة لتأمين حسابك وتفعيل محفظتك الاستثمارية للبدء في تداول الأصول الرقمية بأمان.";

            string otpLabel = isEn ? "Temporary verification code" : "رمز التحقق المؤقت";

            string notesMsg = isEn
                ? "• This code is valid for one-time use only and expires after <strong>15 minutes</strong>.<br>• Never share this code with anyone; the platform team will never ask you for it."
                : "• هذا الرمز صالح للاستخدام لمرة واحدة فقط وينتهي بعد <strong>15 دقيقة</strong>.<br>• لا تشارك هذا الرمز مع أي شخص مطلقاً، فريق عمل المنصة لن يطلب منك هذا الكود.";

            string footerMsg = isEn
                ? "If you haven't created an account on Tibr, you can safely ignore this email."
                : "إذا لم تقم بإنشاء حساب في Tibr، يمكنك تجاهل هذا البريد الإلكتروني بأمان.";

            string emailSubject = isEn ? "Confirm your email - Tibr" : "تأكيد بريدك الإلكتروني - Tibr";

            // بناء قالب الـ HTML النظيف والجاهز للإرسال
            string emailBody = $@"
<div style=""padding: 40px 10px; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;"" dir=""{dir}"">
    <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 550px; background-color: #161b22; border: 1px solid #30363d; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.5);"">
        <!-- Logo Section -->
        <tr>
            <td align=""center"" style=""padding: 30px 20px 20px 20px; border-bottom: 1px solid #21262d;"">
                <img src=""https://ahmedsabry.online/assets/tibrlogo"" alt=""logo"" style=""width: 90px; border-radius: 10px; display: block;"">
            </td>
        </tr>
        
        <!-- Content Section -->
        <tr>
            <td style=""padding: 40px 30px 30px 30px; text-align: {textAlign};"">
                <h2 style=""color: #ffffff; font-size: 20px; font-weight: 600; margin-top: 0; margin-bottom: 16px;"">
                    {welcomeMsg}
                </h2>
                <p style=""color: #c9d1d9; font-size: 15px; line-height: 1.6; margin: 0;"">
                    {descriptionMsg}
                </p>
                
                <!-- OTP Box -->
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin: 35px 0;"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #1f242c 0%, #161b22 100%); border-right: {borderRight}; border-left: {borderLeft}; border-radius: 6px; padding: 25px; text-align: center;"">
                            <span style=""color: #8b949e; font-size: 13px; display: block; margin-bottom: 10px; font-weight: 550;"">
                                {otpLabel}
                            </span>
                            <span style=""color: #d4af37; font-size: 36px; font-weight: 700; letter-spacing: 6px; display: block; font-family: monospace, sans-serif;"">
                                {otp}
                            </span>
                        </td>
                    </tr>
                </table>
                
                <!-- Notes Section -->
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                    <tr>
                        <td style=""color: #8b949e; font-size: 13px; line-height: 1.8; text-align: {textAlign};"">
                            {notesMsg}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        
        <!-- Footer Section -->
        <tr>
            <td style=""padding: 20px 30px; background-color: #0f141c; border-top: 1px solid #21262d; text-align: center;"">
                <p style=""color: #484f58; font-size: 12px; margin: 0 0 10px 0;"">
                   {footerMsg}
                </p>
                <p style=""color: #8b949e; font-size: 11px; margin: 0;"">
                    &copy; 2026 Tibr. All rights reserved.
                </p>
            </td>
        </tr>
    </table>
</div>";

            await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);

            return new AuthResponse(true, "تم إنشاء الحساب بنجاح. يرجى مراجعة بريدك الإلكتروني لتأكيد رمز الـ OTP.", "Account created successfully. Please check your email to confirm your OTP code.");
        }
    }
}
