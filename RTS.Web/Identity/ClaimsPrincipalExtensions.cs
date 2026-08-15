using System.Security.Claims;
using RTS.Application.Identity;

namespace RTS.Web.Identity;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetExternalUserId(
        this ClaimsPrincipal principal,
        out Guid externalUserId)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var value = principal.FindFirstValue(
            ApplicationClaimTypes.ExternalUserId);

        return Guid.TryParse(
            value,
            out externalUserId);
    }
}