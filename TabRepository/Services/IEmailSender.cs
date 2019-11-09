using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(IConfiguration configuration, string email, string subject, string message, string html);
        Task SendEmailAsyncWithAttachment(IConfiguration configuration, string email, string subject, string message, string html, IFormFile file);
    }
}
