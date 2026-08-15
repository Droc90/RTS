using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Email;
using RTS.Application.Identity;
using RTS.Contracts.Identity;

namespace RTS.Infrastructure.Identity;

public sealed class PasswordRecoveryService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IAccountRequestLimiter requestLimiter)
    : IPasswordRecoveryService
{
    public async Task SendResetLinkAsync(
        string email,
        Uri resetPageUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(resetPageUri);

        if (!resetPageUri.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "The reset-page URI must be absolute.",
                nameof(resetPageUri));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!requestLimiter.TryAcquire(
        AccountRequestType.PasswordReset,
        email))
        {
            return;
        }

        var user =
            await userManager.FindByEmailAsync(email.Trim());

        cancellationToken.ThrowIfCancellationRequested();

        if (user is null ||
            !user.IsActive ||
            user.IsDeleted ||
            !user.EmailConfirmed ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var token =
            await userManager.GeneratePasswordResetTokenAsync(user);

        cancellationToken.ThrowIfCancellationRequested();

        var resetUri = BuildResetUri(
            resetPageUri,
            user.ExternalId,
            token);

        var encodedUri =
            HtmlEncoder.Default.Encode(resetUri.AbsoluteUri);

        var message =
            $"""
             <p>A password reset was requested for your Ranked Trading System account.</p>
             <p><a href="{encodedUri}">Reset your password</a></p>
             <p>If you did not request this change, you can ignore this message.</p>
             """;

        await emailSender.SendAsync(
            user.Email,
            "Ranked Trading System - Reset your password",
            message,
            cancellationToken);
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(
        Guid externalUserId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (externalUserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(token))
        {
            return InvalidLinkResult();
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return new ResetPasswordResult(
                false,
                ["A new password is required."]);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.ExternalId == externalUserId,
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.IsDeleted ||
            !user.EmailConfirmed)
        {
            return InvalidLinkResult();
        }

        var result =
            await userManager.ResetPasswordAsync(
                user,
                token,
                newPassword);

        cancellationToken.ThrowIfCancellationRequested();

        if (result.Succeeded)
        {
            return new ResetPasswordResult(
                true,
                Array.Empty<string>());
        }

        if (result.Errors.Any(error =>
            string.Equals(error.Code,
            "InvalidToken",
            StringComparison.Ordinal)))
        {
            return InvalidLinkResult();
        }

        var errors = result.Errors
            .Select(error => error.Description)
            .ToArray();

        return new ResetPasswordResult(
            false,
            errors);
    }

    private static ResetPasswordResult InvalidLinkResult()
    {
        return new ResetPasswordResult(
            false,
            ["The password-reset link is invalid or has expired."]);
    }

    private static Uri BuildResetUri(
        Uri resetPageUri,
        Guid externalUserId,
        string token)
    {
        var uriBuilder = new UriBuilder(resetPageUri);

        var existingQuery =
            uriBuilder.Query.TrimStart('?');

        var resetQuery =
            $"userId={Uri.EscapeDataString(externalUserId.ToString())}" +
            $"&token={Uri.EscapeDataString(token)}";

        uriBuilder.Query = string.IsNullOrWhiteSpace(existingQuery)
            ? resetQuery
            : $"{existingQuery}&{resetQuery}";

        return uriBuilder.Uri;
    }
}
