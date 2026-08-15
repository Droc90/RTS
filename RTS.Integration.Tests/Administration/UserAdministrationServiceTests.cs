using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RTS.Application.Administration;
using RTS.Application.Auditing;
using RTS.Application.Identity;
using RTS.Contracts.Administration;
using RTS.Infrastructure.Administration;
using RTS.Infrastructure.Auditing;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Administration;

public sealed class UserAdministrationServiceTests
{
    [Fact]
    public async Task SearchUsersAsync_ReturnsMatchingUserAndRoles()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var user =
            await CreateUserAsync(
                testScope,
                SystemRoleNames.User);

        var service =
            testScope.Services.GetRequiredService<
                IUserAdministrationService>();

        var result =
            await service.SearchUsersAsync(
                new UserSearchRequest(
                    user.Email,
                    1,
                    25));

        var summary =
            Assert.Single(
                result.Users,
                candidate =>
                    candidate.ExternalId ==
                    user.ExternalId);

        Assert.Equal(user.Email, summary.Email);
        Assert.True(summary.IsActive);
        Assert.False(summary.IsDeleted);

        Assert.Contains(
            SystemRoleNames.User,
            summary.Roles);
    }

    [Fact]
    public async Task SetActiveStatusAsync_DeactivatesUserAndRecordsAudit()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var administrator =
            await CreateUserAsync(
                testScope,
                SystemRoleNames.Administrator);

        var targetUser =
            await CreateUserAsync(
                testScope,
                SystemRoleNames.User);

        var service =
            testScope.Services.GetRequiredService<
                IUserAdministrationService>();

        var result =
            await service.SetActiveStatusAsync(
                targetUser.ExternalId,
                false,
                administrator.ExternalId);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        var context =
            testScope.Services.GetRequiredService<
                RtsDbContext>();

        var persistedUser =
            await context.Users
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id == targetUser.Id);

        Assert.False(persistedUser.IsActive);

        Assert.Equal(
            administrator.Id,
            persistedUser.ModifiedByUserId);

        Assert.NotNull(persistedUser.ModifiedUtc);

        var auditLog =
            await context.AuditLogs
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.EntityExternalId ==
                            targetUser.ExternalId &&
                        item.Action ==
                            AuditAction.UserDeactivated
                                .ToString());

        Assert.Equal(
            administrator.Id,
            auditLog.UserId);

        Assert.True(auditLog.IsSuccess);
        Assert.Contains(
            "\"IsActive\":true",
            auditLog.OldValues);

        Assert.Contains(
            "\"IsActive\":false",
            auditLog.NewValues);
    }

    [Fact]
    public async Task SetActiveStatusAsync_RejectsSelfDeactivation()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var administrator =
            await CreateUserAsync(
                testScope,
                SystemRoleNames.Administrator);

        var service =
            testScope.Services.GetRequiredService<
                IUserAdministrationService>();

        var result =
            await service.SetActiveStatusAsync(
                administrator.ExternalId,
                false,
                administrator.ExternalId);

        Assert.False(result.Succeeded);

        Assert.Contains(
            "You cannot deactivate your own account.",
            result.Errors);

        var context =
            testScope.Services.GetRequiredService<
                RtsDbContext>();

        var persistedAdministrator =
            await context.Users
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                        administrator.Id);

        Assert.True(persistedAdministrator.IsActive);
    }

    private static async Task<ApplicationUser>
        CreateUserAsync(
            TestScope testScope,
            params string[] roles)
    {
        var userManager =
            testScope.Services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var email =
            $"administration-{Guid.NewGuid():N}" +
            "@example.test";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true
        };

        var createResult =
            await userManager.CreateAsync(
                user,
                "AdminTest1!");

        Assert.True(
            createResult.Succeeded,
            string.Join(
                "; ",
                createResult.Errors.Select(
                    error => error.Description)));

        foreach (var role in roles)
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    user,
                    role);

            Assert.True(
                roleResult.Succeeded,
                string.Join(
                    "; ",
                    roleResult.Errors.Select(
                        error => error.Description)));
        }

        return user;
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
            IAuditService,
            AuditService>();

        services.AddScoped<
            IUserAdministrationService,
            UserAdministrationService>();

        var serviceProvider =
            services.BuildServiceProvider();

        var scope =
            serviceProvider.CreateAsyncScope();

        var context =
            scope.ServiceProvider.GetRequiredService<
                RtsDbContext>();

        var transaction =
            await context.Database.BeginTransactionAsync();

        await EnsureRoleAsync(
            scope.ServiceProvider,
            SystemRoleNames.User,
            "Standard application user.");

        await EnsureRoleAsync(
            scope.ServiceProvider,
            SystemRoleNames.Administrator,
            "Application administrator.");

        return new TestScope(
            serviceProvider,
            scope,
            transaction);
    }

    private static async Task EnsureRoleAsync(
        IServiceProvider services,
        string roleName,
        string description)
    {
        var roleManager =
            services.GetRequiredService<
                RoleManager<ApplicationRole>>();

        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result =
            await roleManager.CreateAsync(
                new ApplicationRole
                {
                    Name = roleName,
                    Description = description
                });

        Assert.True(
            result.Succeeded,
            string.Join(
                "; ",
                result.Errors.Select(
                    error => error.Description)));
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