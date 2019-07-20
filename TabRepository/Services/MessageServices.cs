using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        public AuthMessageSender(IOptions<AuthMessageSenderOptions> optionsAccessor)
        {
            Options = optionsAccessor.Value;
        }

        public AuthMessageSenderOptions Options { get; } //set only via Secret Manager
        public Task SendEmailAsync(string email, string subject, string message, string html)
        {
            // Plug in your email service here to send an email.
            return Execute(Options.SendGridKey, subject, message, email, html);
        }

        public Task SendEmailAsyncWithAttachment(string email, string subject, string message, string html, IFormFile file)
        {
            // Plug in your email service here to send an email.
            return Execute(Options.SendGridKey, subject, message, email, html, file);
        }

        public Task Execute(string apiKey, string subject, string message, string email, string html, IFormFile file = null)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("support@tabcollab.com", "TabCollab Support"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = html
            };

            if (file != null)
            {
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    msg.AddAttachment(file.FileName, Convert.ToBase64String(fileBytes));
                }
            }

            msg.AddTo(new EmailAddress(email));
            return client.SendEmailAsync(msg);
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}
