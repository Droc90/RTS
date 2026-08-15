using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RTS.Application.Email;
using RTS.Application.Identity;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Identity;

public sealed class PasswordRecoveryServiceTests
{
    [Fact]
    public async Task SendResetLinkAsync_SendsMessageForEligibleUser()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var user =
            await CreateConfirmedUserAsync(testScope);

        var service =
            testScope.Services.GetRequiredService<
                IPasswordRecoveryService>();

        var emailSender =
            testScope.Services.GetRequiredService<
                TestEmailSender>();

        await service.SendResetLinkAsync(
            user.Email!,
            new Uri(
                "https://rts-web.dev.localhost/" +
                "account/reset-password"));

        var message =
            Assert.Single(emailSender.Messages);

        Assert.Equal(user.Email, message.RecipientEmail);
        Assert.Equal(
            "Ranked Trading System - Reset your password",
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
    public async Task ResetPasswordAsync_ChangesPasswordWithValidToken()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var user =
            await CreateConfirmedUserAsync(testScope);

        var userManager =
            testScope.Services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var service =
            testScope.Services.GetRequiredService<
                IPasswordRecoveryService>();

        var token =
            await userManager
                .GeneratePasswordResetTokenAsync(user);

        var result =
            await service.ResetPasswordAsync(
                user.ExternalId,
                token,
                "Changed2!Test");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        var refreshedUser =
            await userManager.FindByIdAsync(
                user.Id.ToString());

        Assert.NotNull(refreshedUser);

        Assert.False(
            await userManager.CheckPasswordAsync(
                refreshedUser,
                "Original1!Test"));

        Assert.True(
            await userManager.CheckPasswordAsync(
                refreshedUser,
                "Changed2!Test"));
    }

    [Fact]
    public async Task ResetPasswordAsync_RejectsPreviouslyUsedToken()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var user =
            await CreateConfirmedUserAsync(testScope);

        var userManager =
            testScope.Services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var service =
            testScope.Services.GetRequiredService<
                IPasswordRecoveryService>();

        var token =
            await userManager
                .GeneratePasswordResetTokenAsync(user);

        var firstResult =
            await service.ResetPasswordAsync(
                user.ExternalId,
                token,
                "Changed2!Test");

        var reusedResult =
            await service.ResetPasswordAsync(
                user.ExternalId,
                token,
                "Changed3!Test");

        Assert.True(firstResult.Succeeded);
        Assert.False(reusedResult.Succeeded);

        Assert.Contains(
            "The password-reset link is invalid or " +
            "has expired.",
            reusedResult.Errors);
    }

    [Fact]
    public async Task SendResetLinkAsync_DoesNotRevealUnknownAccount()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var service =
            testScope.Services.GetRequiredService<
                IPasswordRecoveryService>();

        var emailSender =
            testScope.Services.GetRequiredService<
                TestEmailSender>();

        await service.SendResetLinkAsync(
            $"unknown-{Guid.NewGuid():N}@example.test",
            new Uri(
                "https://rts-web.dev.localhost/" +
                "account/reset-password"));

        Assert.Empty(emailSender.Messages);
    }

    private static async Task<ApplicationUser>
        CreateConfirmedUserAsync(TestScope testScope)
    {
        var userManager =
            testScope.Services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var email =
            $"recovery-{Guid.NewGuid():N}@example.test";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true
        };

        var result =
            await userManager.CreateAsync(
                user,
                "Original1!Test");

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
            IPasswordRecoveryService,
            PasswordRecoveryService>();

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
