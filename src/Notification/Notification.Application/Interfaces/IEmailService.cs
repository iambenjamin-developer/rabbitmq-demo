namespace Notification.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string subject, string bodyHtml);
    }
}
