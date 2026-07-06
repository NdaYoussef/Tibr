using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task<Result> SendTicketReplyEmailAsync(string toEmail, string clientName, string subject, string message);
    }
}
