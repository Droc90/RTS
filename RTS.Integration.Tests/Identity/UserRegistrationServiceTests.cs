using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RTS.Application.Identity;
using RTS.Contracts.Identity;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Identity;

public sealed class UserRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesNormalizedUserInUserRole()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var service =
            testScope.Services.GetRequiredService<
                IUserRegistrationService>();

        var userManager =
            testScope.Services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var email =
            $"registration-{Guid.NewGuid():N}@example.test";

        var result =
            await service.RegisterAsync(
                new RegisterUserRequest(
                    $"  {email.ToUpperInvariant()}  ",
                    "Register1!Test"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.UserExternalId);
        Assert.Empty(result.Errors);

        var user =
            await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal(email, user.UserName);
        Assert.True(user.IsActive);
        Assert.False(user.IsDeleted);
        Assert.False(user.EmailConfirmed);

        Assert.True(
            await userManager.IsInRoleAsync(
                user,
                SystemRoleNames.User));

        Assert.True(
            await userManager.CheckPasswordAsync(
                user,
                "Register1!Test"));
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateEmail()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var service =
            testScope.Services.GetRequiredService<
                IUserRegistrationService>();

        var email =
            $"registration-{Guid.NewGuid():N}@example.test";

        var firstResult =
            await service.RegisterAsync(
                new RegisterUserRequest(
                    email,
                    "Register1!Test"));

        var duplicateResult =
            await service.RegisterAsync(
                new RegisterUserRequest(
                    email.ToUpperInvariant(),
                    "Register2!Test"));

        Assert.True(firstResult.Succeeded);
        Assert.False(duplicateResult.Succeeded);
        Assert.Null(duplicateResult.UserExternalId);
        Assert.NotEmpty(duplicateResult.Errors);
    }

    [Fact]
    public async Task RegisterAsync_RejectsInvalidPassword()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var service =
            testScope.Services.GetRequiredService<
                IUserRegistrationService>();

        var result =
            await service.RegisterAsync(
                new RegisterUserRequest(
                    $"registration-{Guid.NewGuid():N}" +
                    "@example.test",
                    "weak"));

        Assert.False(result.Succeeded);
        Assert.Null(result.UserExternalId);
        Assert.NotEmpty(result.Errors);
    }

    private static async Task<TestScope>
        CreateTestScopeAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging();

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
            .AddEntityFrameworkStores<RtsDbContext>();

        services.AddScoped<
            IUserRegistrationService,
            UserRegistrationService>();

        var serviceProvider =
            services.BuildServiceProvider();

        var scope =
            serviceProvider.CreateAsyncScope();

        var context =
            scope.ServiceProvider.GetRequiredService<
                RtsDbContext>();

        var transaction =
            await context.Database.BeginTransactionAsync();

        var roleManager =
            scope.ServiceProvider.GetRequiredService<
                RoleManager<ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync(
                SystemRoleNames.User))
        {
            var roleResult =
                await roleManager.CreateAsync(
                    new ApplicationRole
                    {
                        Name = SystemRoleNames.User,
                        Description =
                            "Standard application user."
                    });

            Assert.True(
                roleResult.Succeeded,
                string.Join(
                    "; ",
                    roleResult.Errors.Select(
                        error => error.Description)));
        }

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