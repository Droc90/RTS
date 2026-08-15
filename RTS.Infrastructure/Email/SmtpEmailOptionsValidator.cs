using MailKit.Security;
using Microsoft.Extensions.Options;

namespace RTS.Infrastructure.Email;

public sealed class SmtpEmailOptionsValidator
    : IValidateOptions<SmtpEmailOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        SmtpEmailOptions options)
    {
        var failures = new List<string>();

        var hasUsername =
            !string.IsNullOrWhiteSpace(options.Username);

        var hasPassword =
            !string.IsNullOrWhiteSpace(options.Password);

        if (hasUsername != hasPassword)
        {
            failures.Add(
                "Email:Smtp:Username and Email:Smtp:Password must either both be configured or both be omitted.");
        }

        if (options.SocketOptions is not
            SecureSocketOptions.StartTls and not
            SecureSocketOptions.SslOnConnect)
        {
            failures.Add(
                "Email:Smtp:SocketOptions must be StartTls or SslOnConnect.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}