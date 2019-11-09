using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
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
        public Task SendEmailAsync(IConfiguration configuration, string email, string subject, string message, string html)
        {
            // Plug in your email service here to send an email.
            return Execute(configuration, subject, message, email, html);
        }

        public Task SendEmailAsyncWithAttachment(IConfiguration configuration, string email, string subject, string message, string html, IFormFile file)
        {
            // Plug in your email service here to send an email.
            return Execute(configuration, subject, message, email, html, file);
        }

        public Task Execute(IConfiguration configuration, string subject, string message, string email, string html, IFormFile file = null)
        {
            MailMessage msg = new MailMessage();
            msg.To.Add(new MailAddress(email));
            msg.From = new MailAddress("support@tabcollab.com", "TabCollab Support");
            msg.Subject = subject;
            msg.Body = message;

            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(html);
            htmlView.ContentType = new System.Net.Mime.ContentType("text/html");
            msg.AlternateViews.Add(htmlView);

            SmtpClient client = new SmtpClient();
            client.UseDefaultCredentials = false;            
            client.Credentials = new System.Net.NetworkCredential(configuration["TabCollabEmailCredentials:Email"], configuration["TabCollabEmailCredentials:Password"]);
            client.Port = 587; // Use Port 25 if 587 is blocked
            client.Host = "smtp.office365.com";
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.EnableSsl = true;

            return client.SendMailAsync(msg);           
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}
