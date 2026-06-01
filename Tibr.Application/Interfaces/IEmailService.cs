using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
