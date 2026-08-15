using Microsoft.AspNetCore.Identity;
using RTS.Application.Identity;

namespace RTS.Infrastructure.Identity;

public sealed class RoleInitializationService
    : IRoleInitializationService
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleInitializationService(
        RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureRoleExistsAsync(
            SystemRoleNames.Administrator,
            "Full application administration access.",
            cancellationToken);

        await EnsureRoleExistsAsync(
            SystemRoleNames.User,
            "Standard authenticated application access.",
            cancellationToken);
    }

    private async Task EnsureRoleExistsAsync(
        string roleName,
        string description,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var role = new ApplicationRole
        {
            Name = roleName,
            Description = description,
            IsActive = true
        };

        var result = await _roleManager.CreateAsync(role);

        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => error.Description));

        throw new InvalidOperationException(
            $"Unable to create the '{roleName}' role: {errors}");
    }
}