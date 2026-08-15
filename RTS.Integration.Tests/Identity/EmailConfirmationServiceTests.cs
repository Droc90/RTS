using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RTS.Application.Email;
using RTS.Application.Identity;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Identity;

public sealed class EmailConfirmationServiceTests
{
    [Fact]
    public async Task SendConfirmationAsync_SendsMessageWithConfirmationLink()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var user =
            await CreateUserAsync(testScope);

        var service =
            testScope.Services.GetRequiredService<
                IEmailConfirmationService>();

        var emailSender =
            testScope.Services.GetRequiredService<
                TestEmailSender>();

        await service.SendConfirmationAsync(
            user.ExternalId,
            new Uri(
                "https://rts-web.dev.localhost/" +
                "account/confirm-email"));

        var message =
            Assert.Single(emailSender.Messages);

        Assert.Equal(user.Email, message.RecipientEmail);

        Assert.Equal(
            "Ranked Trading System - Confirm your email address",
            message.Subject);

        Assert.Contains(
            Uri.EscapeDataString(
                user.ExternalId.ToString()),
            message.HtmlMessage);

        Assert.Contains(
            "token=",
            message.HtmlMessage);
    }

    [Fact]
    public async Task ConfirmAsync_ConfirmsEmailWithValidToken()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var user =
            await CreateUserAsync(testScope);

        var userManager =
            testScope.Services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var service =
            testScope.Services.GetRequiredService<
                IEmailConfirmationService>();

        var token =
            await userManager
                .GenerateEmailConfirmationTokenAsync(
                    user);

        var result =
            await service.ConfirmAsync(
                user.ExternalId,
                token);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        var refreshedUser =
            await userManager.FindByIdAsync(
                user.Id.ToString());

        Assert.NotNull(refreshedUser);
        Assert.True(refreshedUser.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmAsync_RejectsInvalidToken()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var user =
            await CreateUserAsync(testScope);

        var service =
            testScope.Services.GetRequiredService<
                IEmailConfirmationService>();

        var result =
            await service.ConfirmAsync(
                user.ExternalId,
                "invalid-token");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task SendConfirmationForEmailAsync_DoesNotRevealUnknownAccount()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var service =
            testScope.Services.GetRequiredService<
                IEmailConfirmationService>();

        var emailSender =
            testScope.Services.GetRequiredService<
                TestEmailSender>();

        await service.SendConfirmationForEmailAsync(
            $"unknown-{Guid.NewGuid():N}@example.test",
            new Uri(
                "https://rts-web.dev.localhost/" +
                "account/confirm-email"));

        Assert.Empty(emailSender.Messages);
    }

    private static async Task<ApplicationUser>
        CreateUserAsync(TestScope testScope)
    {
        var userManager =
            testScope.Services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var email =
            $"confirmation-{Guid.NewGuid():N}" +
            "@example.test";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            IsActive = true
        };

        var result =
            await userManager.CreateAsync(
                user,
                "Confirm1!Test");

        Assert.True(
            result.Succeeded,
            string.Join(
                "; ",
                result.Errors.Select(
                    error => error.Description)));

        return user;
    }

    private static async Task<TestScope>
        CreateTestScopeAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDataProtection();

        services.AddDbContext<RtsDbContext>(
            options =>
                options.UseSqlServer(
                    GetConnectionString()));

        services
            .AddIdentityCore<ApplicationUser>(
                options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.Password.RequiredLength = 10;
                    options.Password.RequiredUniqueChars = 4;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireDigit = true;
                    options.Password
                        .RequireNonAlphanumeric = true;
                })
            .AddRoles<ApplicationRole>()
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<RtsDbContext>();

        services.AddSingleton<TestEmailSender>();

        services.AddSingleton<IEmailSender>(
            provider =>
                provider.GetRequiredService<
                    TestEmailSender>());

        services.AddSingleton<
            IAccountRequestLimiter,
            AllowAllRequestLimiter>();

        services.AddScoped<
            IEmailConfirmationService,
            EmailConfirmationService>();

        var serviceProvider =
            services.BuildServiceProvider();

        var scope =
            serviceProvider.CreateAsyncScope();

        var context =
            scope.ServiceProvider.GetRequiredService<
                RtsDbContext>();

        var transaction =
            await context.Database.BeginTransactionAsync();

        return new TestScope(
            serviceProvider,
            scope,
            transaction);
    }

    private static string GetConnectionString()
    {
        var configuredConnectionString =
            Environment.GetEnvironmentVariable(
                "RTS_TEST_CONNECTION_STRING");

        return string.IsNullOrWhiteSpace(
            configuredConnectionString)
                ? "Server=localhost;Database=RTSIntegrationTests;" +
                  "Trusted_Connection=True;" +
                  "TrustServerCertificate=True;" +
                  "MultipleActiveResultSets=True"
                : configuredConnectionString;
    }

    private sealed class TestEmailSender : IEmailSender
    {
        public List<SentEmail> Messages { get; } = [];

        public Task SendAsync(
            string recipientEmail,
            string subject,
            string htmlMessage,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Messages.Add(
                new SentEmail(
                    recipientEmail,
                    subject,
                    htmlMessage));

            return Task.CompletedTask;
        }
    }

    private sealed record SentEmail(
        string RecipientEmail,
        string Subject,
        string HtmlMessage);

    private sealed class AllowAllRequestLimiter
        : IAccountRequestLimiter
    {
        public bool TryAcquire(
            AccountRequestType requestType,
            string identifier) =>
            true;
    }

    private sealed class TestScope(
        ServiceProvider serviceProvider,
        AsyncServiceScope scope,
        Microsoft.EntityFrameworkCore.Storage
            .IDbContextTransaction transaction)
        : IAsyncDisposable
    {
        public IServiceProvider Services =>
            scope.ServiceProvider;

        public async ValueTask DisposeAsync()
        {
            await transaction.RollbackAsync();
            await transaction.DisposeAsync();
            await scope.DisposeAsync();
            await serviceProvider.DisposeAsync();
        }
    }
}
