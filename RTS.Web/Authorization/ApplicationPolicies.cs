using Microsoft.AspNetCore.Authorization;
using RTS.Application.Authorization;
using RTS.Application.Identity;

namespace RTS.Web.Authorization;

public static class ApplicationPolicies
{
    public static AuthorizationOptions AddApplicationPolicies(
        this AuthorizationOptions options)
    {
        options.AddPolicy(
            ApplicationPolicyNames.RequireAuthenticatedUser,
            policy => policy.RequireAuthenticatedUser());

        options.AddPolicy(
            ApplicationPolicyNames.RequireAdministrator,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(
                    SystemRoleNames.Administrator));

        return options;
    }
}