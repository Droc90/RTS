using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace RTS.Infrastructure.Email;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email:Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public SecureSocketOptions SocketOptions { get; set; } =
        SecureSocketOptions.StartTls;

    public string? Username { get; set; }

    public string? Password { get; set; }

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } =
        string.Empty;

    [Required]
    public string FromName { get; set; } =
        string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}