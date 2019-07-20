using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message, string html);
        Task SendEmailAsyncWithAttachment(string email, string subject, string message, string html, IFormFile file);
    }
}
