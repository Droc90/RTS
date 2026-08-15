using System;
using System.Collections.Generic;
using System.Text;
using RTS.Contracts.Identity;

namespace RTS.Application.Identity;

public interface IUserRegistrationService
{
    Task<RegisterUserResult> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);
}
