using Microsoft.Extensions.Options;
using Notification.Application.Interfaces;
using Notification.Application.Settings;

namespace Notification.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _settings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string subject, string bodyHtml)
        {
            var email = new MimeKit.MimeMessage();
            email.From.Add(new MimeKit.MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            email.To.Add(MimeKit.MailboxAddress.Parse(_settings.To));
            email.Subject = subject;

            var builder = new MimeKit.BodyBuilder { HtmlBody = bodyHtml };
            email.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}