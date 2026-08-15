using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using RTS.Application.Authorization;
using RTS.Application.Identity;
using RTS.Web.Authorization;

namespace RTS.Integration.Tests.Authorization;

public sealed class ApplicationPolicyTests
{
    [Fact]
    public async Task AuthenticatedUserPolicy_RequiresAuthentication()
    {
        using var serviceProvider =
            CreateServiceProvider();

        var authorizationService =
            serviceProvider.GetRequiredService<
                IAuthorizationService>();

        var anonymousResult =
            await authorizationService.AuthorizeAsync(
                CreatePrincipal(),
                null,
                ApplicationPolicyNames
                    .RequireAuthenticatedUser);

        var authenticatedResult =
            await authorizationService.AuthorizeAsync(
                CreatePrincipal("User"),
                null,
                ApplicationPolicyNames
                    .RequireAuthenticatedUser);

        Assert.False(anonymousResult.Succeeded);
        Assert.True(authenticatedResult.Succeeded);
    }

    [Fact]
    public async Task AdministratorPolicy_RequiresAdministratorRole()
    {
        using var serviceProvider =
            CreateServiceProvider();

        var authorizationService =
            serviceProvider.GetRequiredService<
                IAuthorizationService>();

        var anonymousResult =
            await authorizationService.AuthorizeAsync(
                CreatePrincipal(),
                null,
                ApplicationPolicyNames
                    .RequireAdministrator);

        var userResult =
            await authorizationService.AuthorizeAsync(
                CreatePrincipal(
                    SystemRoleNames.User),
                null,
                ApplicationPolicyNames
                    .RequireAdministrator);

        var administratorResult =
            await authorizationService.AuthorizeAsync(
                CreatePrincipal(
                    SystemRoleNames.Administrator),
                null,
                ApplicationPolicyNames
                    .RequireAdministrator);

        Assert.False(anonymousResult.Succeeded);
        Assert.False(userResult.Succeeded);
        Assert.True(administratorResult.Succeeded);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddAuthorization(
            options =>
                options.AddApplicationPolicies());

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal CreatePrincipal(
        string? role = null)
    {
        if (role is null)
        {
            return new ClaimsPrincipal(
                new ClaimsIdentity());
        }

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                Guid.NewGuid().ToString("D")),
            new Claim(
                ClaimTypes.Role,
                role)
        };

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                authenticationType: "Test"));
    }
}