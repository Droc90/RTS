using Microsoft.AspNetCore.Identity;
using RTS.Application.Identity;

namespace RTS.Infrastructure.Identity;

public sealed class InitialAdministratorService
    : IInitialAdministratorService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public InitialAdministratorService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task PromoteAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Administrator email is required.",
                nameof(email));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var existingAdministrators =
            await _userManager.GetUsersInRoleAsync(
                SystemRoleNames.Administrator);

        if (existingAdministrators.Count > 0)
        {
            return;
        }

        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        var user = await _userManager.FindByEmailAsync(
            normalizedEmail);

        if (user is null)
        {
            throw new InvalidOperationException(
                $"The initial administrator account " +
                $"'{normalizedEmail}' was not found.");
        }

        if (!user.IsActive || user.IsDeleted)
        {
            throw new InvalidOperationException(
                $"The initial administrator account " +
                $"'{normalizedEmail}' is inactive or deleted.");
        }

        await EnsureUserHasRoleAsync(
            user,
            SystemRoleNames.User);

        await EnsureUserHasRoleAsync(
            user,
            SystemRoleNames.Administrator);
    }

    private async Task EnsureUserHasRoleAsync(
        ApplicationUser user,
        string roleName)
    {
        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            return;
        }

        var result = await _userManager.AddToRoleAsync(
            user,
            roleName);

        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => error.Description));

        throw new InvalidOperationException(
            $"Unable to add '{user.Email}' to the " +
            $"'{roleName}' role: {errors}");
    }
}