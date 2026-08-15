using RTS.Contracts.Identity;

namespace RTS.Application.Identity;

public interface IPasswordRecoveryService
{
    Task SendResetLinkAsync(
        string email,
        Uri resetPageUri,
        CancellationToken cancellationToken = default);

    Task<ResetPasswordResult> ResetPasswordAsync(
        Guid externalUserId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}