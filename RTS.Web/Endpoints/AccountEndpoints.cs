using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using RTS.Infrastructure.Identity;
using RTS.Application.Auditing;

namespace RTS.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/login/submit", LoginAsync);
        endpoints.MapPost("/account/logout", LogoutAsync);

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILoginHistoryService loginHistoryService)
    {
        await antiforgery.ValidateRequestAsync(context);

        var form = await context.Request.ReadFormAsync(
            context.RequestAborted);

        var email = form["Email"]
            .ToString()
            .Trim()
            .ToLowerInvariant();

        var password = form["Password"].ToString();

        var rememberMe = string.Equals(
            form["RememberMe"].ToString(),
            "true",
            StringComparison.OrdinalIgnoreCase);

        var returnUrl = GetSafeReturnUrl(
            form["ReturnUrl"].ToString());

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            await RecordLoginEventAsync(
                loginHistoryService,
                context,
                null,
                email,
                LoginEventType.LoginFailed,
                false,
                LoginFailureCode.UserNotFound);

            return RedirectToLogin("invalid", returnUrl);
        }

        if (user.IsDeleted)
        {
            await RecordLoginEventAsync(
                loginHistoryService,
                context,
                user,
                email,
                LoginEventType.LoginFailed,
                false,
                LoginFailureCode.AccountDeleted);

            return RedirectToLogin("invalid", returnUrl);
        }

        if (!user.IsActive)
        {
            await RecordLoginEventAsync(
                loginHistoryService,
                context,
                user,
                email,
                LoginEventType.LoginFailed,
                false,
                LoginFailureCode.AccountInactive);

            return RedirectToLogin("invalid", returnUrl);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            password,
            rememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginUtc = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            await RecordLoginEventAsync(
                loginHistoryService,
                context,
                user,
                email,
                LoginEventType.LoginSucceeded,
                true);

            return Results.LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            await RecordLoginEventAsync(
                loginHistoryService,
                context,
                user,
                email,
                LoginEventType.LoginLockedOut,
                false,
                LoginFailureCode.AccountLockedOut);

            return RedirectToLogin("locked", returnUrl);
        }

        var failureCode = result.IsNotAllowed
        ? LoginFailureCode.SignInNotAllowed
        : LoginFailureCode.InvalidCredentials;

            await RecordLoginEventAsync(
                loginHistoryService,
                context,
                user,
                email,
                LoginEventType.LoginFailed,
                false,
                failureCode);

        return RedirectToLogin("invalid", returnUrl);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILoginHistoryService loginHistoryService)
    {
        await antiforgery.ValidateRequestAsync(context);

        var form = await context.Request.ReadFormAsync(
            context.RequestAborted);

        ApplicationUser? user = null;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            user = await userManager.GetUserAsync(context.User);
        }

        await signInManager.SignOutAsync();

        if (user is not null)
        {
            await RecordLoginEventAsync(
                loginHistoryService,
                context,
                user,
                user.Email,
                LoginEventType.LogoutSucceeded,
                true);
        }

        var returnUrl = GetSafeReturnUrl(
            form["ReturnUrl"].ToString());

        return Results.LocalRedirect(returnUrl);
    }

    private static IResult RedirectToLogin(
        string error,
        string returnUrl)
    {
        var location = QueryHelpers.AddQueryString(
            "/account/login",
            new Dictionary<string, string?>
            {
                ["error"] = error,
                ["returnUrl"] = returnUrl
            });

        return Results.Redirect(location);
    }

    private static Task RecordLoginEventAsync(
    ILoginHistoryService loginHistoryService,
    HttpContext context,
    ApplicationUser? user,
    string? identifier,
    LoginEventType eventType,
    bool isSuccess,
    LoginFailureCode? failureCode = null)
    {
        var request = new RecordLoginHistoryRequest(
            user?.ExternalId,
            identifier,
            eventType,
            isSuccess,
            failureCode,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString(),
            Guid.NewGuid());

        return loginHistoryService.RecordAsync(
            request,
            context.RequestAborted);
    }
    private static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !returnUrl.StartsWith('/') ||
            returnUrl.StartsWith("//") ||
            returnUrl.StartsWith("/\\") ||
            !Uri.IsWellFormedUriString(
                returnUrl,
                UriKind.Relative))
        {
            return "/dashboard";
        }

        return returnUrl;
    }
}
