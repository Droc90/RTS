using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using RTS.Application.Email;

namespace RTS.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpEmailOptions> options)
    : IEmailSender
{
    private readonly SmtpEmailOptions _options =
        options.Value;

    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlMessage,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            recipientEmail);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            subject);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            htmlMessage);

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _options.FromName,
                _options.FromAddress));

        message.To.Add(
            MailboxAddress.Parse(recipientEmail));

        message.Subject = subject;

        message.Body = new TextPart(TextFormat.Html)
        {
            Text = htmlMessage
        };

        using var client = new SmtpClient
        {
            Timeout = checked(
                _options.TimeoutSeconds * 1000)
        };

        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            _options.SocketOptions,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(
            _options.Username))
        {
            var password =
                _options.Password
                ?? throw new InvalidOperationException(
                    "The SMTP password is not configured.");

            await client.AuthenticateAsync(
                _options.Username,
                password,
                cancellationToken);
        }

        await client.SendAsync(
            message,
            cancellationToken: cancellationToken);

        await client.DisconnectAsync(
            quit: true,
            cancellationToken);
    }
}