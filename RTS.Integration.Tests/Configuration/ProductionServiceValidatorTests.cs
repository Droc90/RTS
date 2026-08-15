using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RTS.Application.Email;
using RTS.Web.Configuration;

namespace RTS.Integration.Tests.Configuration;

public sealed class ProductionServiceValidatorTests
{
    [Fact]
    public void Validate_AllowsMissingEmailSenderInDevelopment()
    {
        using var services =
            new ServiceCollection()
                .BuildServiceProvider();

        var exception = Record.Exception(() =>
            ProductionServiceValidator.Validate(
                CreateEnvironment(
                    Environments.Development),
                services));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_RejectsMissingEmailSenderInProduction()
    {
        using var services =
            new ServiceCollection()
                .BuildServiceProvider();

        var exception = Assert.Throws<
            InvalidOperationException>(() =>
                ProductionServiceValidator.Validate(
                    CreateEnvironment(
                        Environments.Production),
                    services));

        Assert.Contains(
            nameof(IEmailSender),
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_AcceptsRegisteredEmailSenderInProduction()
    {
        using var services =
            new ServiceCollection()
                .AddSingleton<
                    IEmailSender,
                    TestEmailSender>()
                .BuildServiceProvider();

        var exception = Record.Exception(() =>
            ProductionServiceValidator.Validate(
                CreateEnvironment(
                    Environments.Production),
                services));

        Assert.Null(exception);
    }

    private static IHostEnvironment CreateEnvironment(
        string environmentName)
    {
        return new TestHostEnvironment
        {
            EnvironmentName = environmentName
        };
    }

    private sealed class TestEmailSender
        : IEmailSender
    {
        public Task SendAsync(
            string recipientEmail,
            string subject,
            string htmlMessage,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
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