using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using RTS.Application.Identity;
using RTS.Contracts.Identity;

namespace RTS.Infrastructure.Identity;

public sealed class UserRegistrationService
    : IUserRegistrationService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRegistrationService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<RegisterUserResult> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email.Trim().ToLowerInvariant();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(error => error.Description)
                .ToArray();

            return new RegisterUserResult(
                false,
                null,
                errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            SystemRoleNames.User);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            var errors = roleResult.Errors
                .Select(error => error.Description)
                .ToArray();

            return new RegisterUserResult(
                false,
                null,
                errors);
        }

        return new RegisterUserResult(
            true,
            user.ExternalId,
            Array.Empty<string>());
    }
}
