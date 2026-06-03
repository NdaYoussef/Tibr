using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Tibr.Application.Interfaces;
using MailKit.Net.Smtp;

namespace Tibr.Application.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress
                (
                _configuration["EmailSettings:SenderName"],
                _configuration["EmailSettings:SenderEmail"]!
                ));

            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            // إعداد محتوى الرسالة يدعم تنسيق HTML لتظهر بشكل فاخر ومناسب لهوية المنصة
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync
                    (
                    _configuration["EmailSettings:Host"]!,
                    int.Parse(_configuration["EmailSettings:Port"]!
                    ),
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync
                    (
                    _configuration["EmailSettings:SenderEmail"]!,
                    _configuration["EmailSettings:Password"]!
                    );

                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
