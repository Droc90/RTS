using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Email;
using RTS.Application.Identity;
using RTS.Contracts.Identity;

namespace RTS.Infrastructure.Identity;

public sealed class EmailConfirmationService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IAccountRequestLimiter requestLimiter)
    : IEmailConfirmationService
{
    public async Task SendConfirmationAsync(
        Guid externalUserId,
        Uri confirmationPageUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(confirmationPageUri);

        if (!confirmationPageUri.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "The confirmation-page URI must be absolute.",
                nameof(confirmationPageUri));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.ExternalId == externalUserId,
            cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException(
                "The account requiring email confirmation was not found.");
        }

        if (!user.IsActive || user.IsDeleted)
        {
            throw new InvalidOperationException(
                "Email confirmation cannot be sent for an inactive account.");
        }

        if (user.EmailConfirmed)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException(
                "The account does not have an email address.");
        }

        var token =
            await userManager.GenerateEmailConfirmationTokenAsync(user);

        cancellationToken.ThrowIfCancellationRequested();


        var confirmationUri = BuildConfirmationUri(
            confirmationPageUri,
            user.ExternalId,
            token);

        var encodedUri =
            HtmlEncoder.Default.Encode(confirmationUri.AbsoluteUri);

        var message =
            $"""
             <p>Please confirm your Ranked Trading System email address by selecting the link below.</p>
             <p><a href="{encodedUri}">Confirm email address</a></p>
             <p>If you did not create this account, you can ignore this message.</p>
             """;

        await emailSender.SendAsync(
            user.Email,
            "Ranked Trading System - Confirm your email address",
            message,
            cancellationToken);
    }

    public async Task SendConfirmationForEmailAsync(
    string email,
    Uri confirmationPageUri,
    CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(confirmationPageUri);

        cancellationToken.ThrowIfCancellationRequested();

        if (!requestLimiter.TryAcquire(
        AccountRequestType.EmailConfirmation,
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
            user.EmailConfirmed)
        {
            return;
        }

        await SendConfirmationAsync(
            user.ExternalId,
            confirmationPageUri,
            cancellationToken);
    }

    public async Task<ConfirmEmailResult> ConfirmAsync(
    Guid externalUserId,
    string token,
    CancellationToken cancellationToken = default)
    {
        if (externalUserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(token))
        {
            return new ConfirmEmailResult(
                false,
                ["The email-confirmation link is invalid."]);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.ExternalId == externalUserId,
            cancellationToken);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            return new ConfirmEmailResult(
                false,
                ["The email-confirmation link is invalid."]);
        }

        if (user.EmailConfirmed)
        {
            return new ConfirmEmailResult(
                true,
                Array.Empty<string>());
        }

        var result =
            await userManager.ConfirmEmailAsync(user, token);

        cancellationToken.ThrowIfCancellationRequested();

        if (result.Succeeded)
        {
            return new ConfirmEmailResult(
                true,
                Array.Empty<string>());
        }

        var errors = result.Errors
            .Select(error => error.Description)
            .ToArray();

        return new ConfirmEmailResult(
            false,
            errors);
    }
    private static Uri BuildConfirmationUri(
        Uri confirmationPageUri,
        Guid externalUserId,
        string token)
    {
        var uriBuilder = new UriBuilder(confirmationPageUri);

        var existingQuery =
            uriBuilder.Query.TrimStart('?');

        var confirmationQuery =
            $"userId={Uri.EscapeDataString(externalUserId.ToString())}" +
            $"&token={Uri.EscapeDataString(token)}";

        uriBuilder.Query = string.IsNullOrWhiteSpace(existingQuery)
            ? confirmationQuery
            : $"{existingQuery}&{confirmationQuery}";

        return uriBuilder.Uri;
    }
}
