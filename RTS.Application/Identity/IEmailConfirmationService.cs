using RTS.Contracts.Identity;

namespace RTS.Application.Identity;

public interface IEmailConfirmationService
{
    Task SendConfirmationAsync(
        Guid externalUserId,
        Uri confirmationPageUri,
        CancellationToken cancellationToken = default);

    Task SendConfirmationForEmailAsync(
    string email,
    Uri confirmationPageUri,
    CancellationToken cancellationToken = default);

    Task<ConfirmEmailResult> ConfirmAsync(
        Guid externalUserId,
        string token,
        CancellationToken cancellationToken = default);
}