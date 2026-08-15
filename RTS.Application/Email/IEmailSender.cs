namespace RTS.Application.Email;

public interface IEmailSender
{
    Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlMessage,
        CancellationToken cancellationToken = default);
}