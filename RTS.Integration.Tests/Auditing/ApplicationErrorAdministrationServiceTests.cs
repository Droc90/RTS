using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RTS.Application.Auditing;
using RTS.Application.Identity;
using RTS.Contracts.Auditing;
using RTS.Infrastructure.Auditing;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Auditing;

public sealed class ApplicationErrorAdministrationServiceTests
{
    [Fact]
    public async Task SearchAndGetAsync_ReturnMatchingError()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var context =
            testScope.Services.GetRequiredService<
                RtsDbContext>();

        var correlationId =
            Guid.NewGuid();

        var errorCode =
            $"TEST_{Guid.NewGuid():N}";

        var applicationError =
            CreateApplicationError(
                correlationId,
                errorCode);

        context.ApplicationErrors.Add(
            applicationError);

        await context.SaveChangesAsync();

        var service =
            testScope.Services.GetRequiredService<
                IApplicationErrorAdministrationService>();

        var searchResult =
            await service.SearchAsync(
                new ApplicationErrorSearchRequest(
                    correlationId.ToString("D"),
                    ApplicationErrorResolutionStatuses.New,
                    1,
                    25));

        var summary =
            Assert.Single(
                searchResult.Errors,
                candidate =>
                    candidate.ExternalId ==
                    applicationError.ExternalId);

        Assert.Equal(
            correlationId,
            summary.CorrelationId);

        Assert.Equal(
            errorCode,
            summary.ErrorCode);

        var details =
            await service.GetAsync(
                applicationError.ExternalId);

        Assert.NotNull(details);

        Assert.Equal(
            "Integration-test diagnostic details.",
            details.DiagnosticDetails);

        Assert.Equal(
            ApplicationErrorResolutionStatuses.New,
            details.ResolutionStatus);

        Assert.NotEmpty(details.RowVersion);
    }

    [Fact]
    public async Task UpdateAsync_ResolvesErrorAndRecordsAdministrator()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var administrator =
            await CreateAdministratorAsync(
                testScope);

        var context =
            testScope.Services.GetRequiredService<
                RtsDbContext>();

        var applicationError =
            CreateApplicationError(
                Guid.NewGuid(),
                $"TEST_{Guid.NewGuid():N}");

        context.ApplicationErrors.Add(
            applicationError);

        await context.SaveChangesAsync();

        var originalRowVersion =
            applicationError.RowVersion.ToArray();

        var service =
            testScope.Services.GetRequiredService<
                IApplicationErrorAdministrationService>();

        var result =
            await service.UpdateAsync(
                applicationError.ExternalId,
                administrator.ExternalId,
                new UpdateApplicationErrorRequest(
                    ApplicationErrorResolutionStatuses.Resolved,
                    "Verified and resolved.",
                    originalRowVersion));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Error);

        Assert.Equal(
            ApplicationErrorResolutionStatuses.Resolved,
            result.Error.ResolutionStatus);

        Assert.Equal(
            "Verified and resolved.",
            result.Error.ResolutionNotes);

        Assert.NotNull(result.Error.ResolvedUtc);

        Assert.Equal(
            administrator.Email,
            result.Error.ResolvedByUserEmail);

        Assert.False(
            originalRowVersion.SequenceEqual(
                result.Error.RowVersion));
    }

    [Fact]
    public async Task UpdateAsync_RejectsStaleRowVersion()
    {
        await using var testScope =
            await CreateTestScopeAsync();

        var administrator =
            await CreateAdministratorAsync(
                testScope);

        var context =
            testScope.Services.GetRequiredService<
                RtsDbContext>();

        var applicationError =
            CreateApplicationError(
                Guid.NewGuid(),
                $"TEST_{Guid.NewGuid():N}");

        context.ApplicationErrors.Add(
            applicationError);

        await context.SaveChangesAsync();

        var staleRowVersion =
            applicationError.RowVersion.ToArray();

        var service =
            testScope.Services.GetRequiredService<
                IApplicationErrorAdministrationService>();

        var firstUpdate =
            await service.UpdateAsync(
                applicationError.ExternalId,
                administrator.ExternalId,
                new UpdateApplicationErrorRequest(
                    ApplicationErrorResolutionStatuses
                        .Investigating,
                    "Investigation started.",
                    applicationError.RowVersion));

        Assert.True(firstUpdate.Succeeded);

        var staleUpdate =
            await service.UpdateAsync(
                applicationError.ExternalId,
                administrator.ExternalId,
                new UpdateApplicationErrorRequest(
                    ApplicationErrorResolutionStatuses.Resolved,
                    "Stale resolution attempt.",
                    staleRowVersion));

        Assert.False(staleUpdate.Succeeded);

        Assert.Equal(
            "ConcurrencyConflict",
            staleUpdate.ErrorCode);
    }

    private static ApplicationError
        CreateApplicationError(
            Guid correlationId,
            string errorCode) =>
        new()
        {
            CorrelationId = correlationId,
            ErrorType =
                "System.InvalidOperationException",
            ErrorCode = errorCode,
            SafeMessage =
                "An unexpected error occurred.",
            DiagnosticDetails =
                "Integration-test diagnostic details.",
            Source =
                "ApplicationErrorAdministrationServiceTests",
            RequestPath =
                "/integration-test/error-administration",
            HttpMethod = "GET",
            StatusCode = 500,
            ResolutionStatus =
                ApplicationErrorResolutionStatuses.New
        };

    private static async Task<ApplicationUser>
        CreateAdministratorAsync(
            TestScope testScope)
    {
        var userManager =
            testScope.Services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var email =
            $"error-admin-{Guid.NewGuid():N}" +
            "@example.test";

        var administrator =
            new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true
            };

        var createResult =
            await userManager.CreateAsync(
                administrator,
                "ErrorAdmin1!");

        Assert.True(
            createResult.Succeeded,
            string.Join(
                "; ",
                createResult.Errors.Select(
                    error => error.Description)));

        var roleResult =
            await userManager.AddToRoleAsync(
                administrator,
                SystemRoleNames.Administrator);

        Assert.True(
            roleResult.Succeeded,
            string.Join(
                "; ",
                roleResult.Errors.Select(
                    error => error.Description)));

        return administrator;
    }

    private static async Task<TestScope>
        CreateTestScopeAsync()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<RtsDbContext>(
            options =>
                options.UseSqlServer(
                    GetConnectionString()));

        services
            .AddIdentityCore<ApplicationUser>(
                options =>
                {
                    options.User.RequireUniqueEmail =
                        true;

                    options.Password.RequiredLength =
                        10;

                    options.Password
                        .RequiredUniqueChars = 4;

                    options.Password
                        .RequireUppercase = true;

                    options.Password
                        .RequireLowercase = true;

                    options.Password
                        .RequireDigit = true;

                    options.Password
                        .RequireNonAlphanumeric = true;
                })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<RtsDbContext>();

        services.AddScoped<
            IApplicationErrorAdministrationService,
            ApplicationErrorAdministrationService>();

        var serviceProvider =
            services.BuildServiceProvider();

        var scope =
            serviceProvider.CreateAsyncScope();

        var context =
            scope.ServiceProvider.GetRequiredService<
                RtsDbContext>();

        var transaction =
            await context.Database
                .BeginTransactionAsync();

        await EnsureAdministratorRoleAsync(
            scope.ServiceProvider);

        return new TestScope(
            serviceProvider,
            scope,
            transaction);
    }

    private static async Task
        EnsureAdministratorRoleAsync(
            IServiceProvider services)
    {
        var roleManager =
            services.GetRequiredService<
                RoleManager<ApplicationRole>>();

        if (await roleManager.RoleExistsAsync(
                SystemRoleNames.Administrator))
        {
            return;
        }

        var result =
            await roleManager.CreateAsync(
                new ApplicationRole
                {
                    Name =
                        SystemRoleNames.Administrator,
                    Description =
                        "Application administrator."
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