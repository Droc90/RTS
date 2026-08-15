using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RTS.Application.Identity;
using RTS.Infrastructure.Identity;

namespace RTS.Web.Identity;

public sealed class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<
        ApplicationUser,
        ApplicationRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(
            userManager,
            roleManager,
            optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity>
        GenerateClaimsAsync(ApplicationUser user)
    {
        var identity =
            await base.GenerateClaimsAsync(user);

        identity.AddClaim(
            new Claim(
                ApplicationClaimTypes.ExternalUserId,
                user.ExternalId.ToString("D")));

        return identity;
    }
}