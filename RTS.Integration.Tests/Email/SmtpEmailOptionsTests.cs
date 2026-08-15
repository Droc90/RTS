using System.ComponentModel.DataAnnotations;
using MailKit.Security;
using RTS.Infrastructure.Email;

namespace RTS.Integration.Tests.Email;

public sealed class SmtpEmailOptionsTests
{
    [Fact]
    public void Validate_AcceptsAuthenticatedSmtp()
    {
        var options = CreateValidOptions();
        options.Username = "smtp-user";
        options.Password = "smtp-password";

        var result =
            new SmtpEmailOptionsValidator()
                .Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_AcceptsTrustedSmtpRelay()
    {
        var options = CreateValidOptions();

        var result =
            new SmtpEmailOptionsValidator()
                .Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("smtp-user", null)]
    [InlineData(null, "smtp-password")]
    public void Validate_RejectsPartialCredentials(
        string? username,
        string? password)
    {
        var options = CreateValidOptions();
        options.Username = username;
        options.Password = password;

        var result =
            new SmtpEmailOptionsValidator()
                .Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "both be configured",
                StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(SecureSocketOptions.None)]
    [InlineData(SecureSocketOptions.Auto)]
    [InlineData(
        SecureSocketOptions.StartTlsWhenAvailable)]
    public void Validate_RejectsNonStrictTransportSecurity(
        SecureSocketOptions socketOptions)
    {
        var options = CreateValidOptions();
        options.SocketOptions = socketOptions;

        var result =
            new SmtpEmailOptionsValidator()
                .Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "StartTls or SslOnConnect",
                StringComparison.Ordinal));
    }

    [Fact]
    public void DataAnnotations_RejectInvalidSenderAddress()
    {
        var options = CreateValidOptions();
        options.FromAddress = "not-an-email-address";

        var validationResults =
            new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(
            validationResults,
            result => result.MemberNames.Contains(
                nameof(SmtpEmailOptions.FromAddress)));
    }

    private static SmtpEmailOptions CreateValidOptions()
    {
        return new SmtpEmailOptions
        {
            Host = "smtp.example.com",
            Port = 587,
            SocketOptions =
                SecureSocketOptions.StartTls,
            FromAddress = "no-reply@example.com",
            FromName = "Ranked Trading System",
            TimeoutSeconds = 30
        };
    }
}
