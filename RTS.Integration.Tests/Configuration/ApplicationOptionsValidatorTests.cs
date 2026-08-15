using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RTS.Web.Configuration;

namespace RTS.Integration.Tests.Configuration;

public sealed class ApplicationOptionsValidatorTests
{
    [Fact]
    public void Validate_AllowsMissingKeyPathInDevelopment()
    {
        var validator = CreateValidator(
            Environments.Development);

        var result = validator.Validate(
            null,
            CreateValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsMissingKeyPathInProduction()
    {
        var validator = CreateValidator(
            Environments.Production);

        var result = validator.Validate(
            null,
            CreateValidOptions());

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "DataProtectionKeysPath",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsRelativeKeyPathInProduction()
    {
        var validator = CreateValidator(
            Environments.Production);

        var options = CreateValidOptions();
        options.DataProtectionKeysPath = "keys";

        var result = validator.Validate(
            null,
            options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "absolute path",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_AcceptsValidProductionConfiguration()
    {
        var validator = CreateValidator(
            Environments.Production);

        var options = CreateValidOptions();
        options.DataProtectionKeysPath =
            Path.GetFullPath("rts-keys");

        var result = validator.Validate(
            null,
            options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsWildcardHostsInProduction()
    {
        var validator = CreateValidator(
            Environments.Production,
            allowedHosts: "*");

        var options = CreateValidOptions();
        options.DataProtectionKeysPath =
            Path.GetFullPath("rts-keys");

        var result = validator.Validate(
            null,
            options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "AllowedHosts",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsCertificatePasswordWithoutPath()
    {
        var validator = CreateValidator(
            Environments.Production);

        var options = CreateValidOptions();
        options.DataProtectionKeysPath =
            Path.GetFullPath("rts-keys");

        options.DataProtectionCertificatePath = null;
        options.DataProtectionCertificatePassword =
            "test-password";

        var result = validator.Validate(
            null,
            options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "DataProtectionCertificatePath",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsRelativeCertificatePath()
    {
        var validator = CreateValidator(
            Environments.Production);

        var options = CreateValidOptions();
        options.DataProtectionKeysPath =
            Path.GetFullPath("rts-keys");

        options.DataProtectionCertificatePath =
            "rts-certificate.pfx";

        var result = validator.Validate(
            null,
            options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "certificate",
                StringComparison.OrdinalIgnoreCase) &&
                failure.Contains(
                    "absolute path",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationOptionsValidator CreateValidator(
        string environmentName,
        string allowedHosts = "rts.example.com")
    {
        var configurationValues =
            new Dictionary<string, string?>
            {
                ["AllowedHosts"] = allowedHosts
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

        return new ApplicationOptionsValidator(
            new TestHostEnvironment
            {
                EnvironmentName = environmentName
            },
            configuration);
    }

    private static ApplicationOptions CreateValidOptions()
    {
        return new ApplicationOptions
        {
            Name = "RTS",
            DisplayName = "Ranked Trading System",
            CookieName = ".RTS.Auth",
            DataProtectionName = "RTS",
            DataProtectionCertificatePath =
                Path.GetFullPath(
                    "rts-certificate.pfx")
        };
    }

    private sealed class TestHostEnvironment
        : IHostEnvironment
    {
        public string EnvironmentName { get; set; } =
            Environments.Development;

        public string ApplicationName { get; set; } =
            "RTS.Integration.Tests";

        public string ContentRootPath { get; set; } =
            Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider
        {
            get;
            set;
        } = new NullFileProvider();
    }
}
