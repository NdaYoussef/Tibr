using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Tibr.Application.Interfaces;
using MailKit.Net.Smtp;
using Tibr.Domain.ResultPattern;

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

            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            try
            { 
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
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }

        public async Task<Result> SendTicketReplyEmailAsync(string toEmail, string clientName, string subject, string message)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_configuration["EmailSettings:SenderEmail"]!));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = $"Reply sent successfully: {subject}";

                var builder = new BodyBuilder
                {
                    HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; direction: rtl;'>
                    <p>Hello {clientName}،</p>
                    <p>Reply has been sent successfully</p>
                    <blockquote style='border-right: 3px solid #ccc; padding-right: 10px;'>{message}</blockquote>
                    <p>Thanks for Contacting Us</p>
                </div>"
                };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _configuration["EmailSettings:Host"]!,
                    int.Parse(_configuration["EmailSettings:Port"]!),
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _configuration["EmailSettings:SenderEmail"]!,
                    _configuration["EmailSettings:Password"]!);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to send email: {ex.Message}");
            }
        
    }
    }
}
