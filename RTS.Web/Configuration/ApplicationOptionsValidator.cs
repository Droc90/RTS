using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace RTS.Web.Configuration;

public sealed class ApplicationOptionsValidator
    : IValidateOptions<ApplicationOptions>
{
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ApplicationOptionsValidator(
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public ValidateOptionsResult Validate(
        string? name,
        ApplicationOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            failures.Add(
                "Application:Name is required.");
        }

        if (string.IsNullOrWhiteSpace(options.DisplayName))
        {
            failures.Add(
                "Application:DisplayName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.CookieName))
        {
            failures.Add(
                "Application:CookieName is required.");
        }

        if (string.IsNullOrWhiteSpace(
            options.DataProtectionName))
        {
            failures.Add(
                "Application:DataProtectionName is required.");
        }

        if (!_environment.IsDevelopment())
        {
            ValidateProductionSettings(
                options,
                failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private void ValidateProductionSettings(
        ApplicationOptions options,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(
            options.DataProtectionKeysPath))
        {
            failures.Add(
                "Application:DataProtectionKeysPath is required outside Development.");
        }
        else if (!Path.IsPathRooted(
            options.DataProtectionKeysPath))
        {
            failures.Add(
                "Application:DataProtectionKeysPath must be an absolute path.");
        }

        ValidateDataProtectionCertificate(
            options,
            failures);

        var allowedHosts =
            _configuration["AllowedHosts"];

        if (string.IsNullOrWhiteSpace(allowedHosts) ||
            allowedHosts.Trim() == "*")
        {
            failures.Add(
                "AllowedHosts must contain explicit production host names.");
        }
    }

    private static void ValidateDataProtectionCertificate(
        ApplicationOptions options,
        ICollection<string> failures)
    {
        var certificatePath =
            options.DataProtectionCertificatePath;

        var certificatePassword =
            options.DataProtectionCertificatePassword;

        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            if (!string.IsNullOrWhiteSpace(
                certificatePassword))
            {
                failures.Add(
                    "Application:DataProtectionCertificatePath is required when a certificate password is configured.");
            }

            if (!OperatingSystem.IsWindows())
            {
                failures.Add(
                    "Application:DataProtectionCertificatePath is required for production Data Protection outside Windows.");
            }

            return;
        }

        if (!Path.IsPathRooted(certificatePath))
        {
            failures.Add(
                "Application:DataProtectionCertificatePath must be an absolute path.");
        }
    }
}