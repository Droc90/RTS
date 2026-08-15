using Microsoft.Extensions.Logging;
using RTS.Application.Email;
using System.Net;

namespace RTS.Infrastructure.Email;

public sealed class DevelopmentEmailSender(
    ILogger<DevelopmentEmailSender> logger)
    : IEmailSender
{
    public Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlMessage,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlMessage);

        var developmentMessage =
            WebUtility.HtmlDecode(htmlMessage);

        logger.LogInformation(
            """
            Development email generated.
            Recipient: {RecipientEmail}
            Subject: {Subject}
            Message:
            {HtmlMessage}
            """,
            recipientEmail,
            subject,
            developmentMessage);

        return Task.CompletedTask;
    }
}