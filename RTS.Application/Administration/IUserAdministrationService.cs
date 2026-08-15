using RTS.Contracts.Administration;

namespace RTS.Application.Administration;

public interface IUserAdministrationService
{
    Task<UserSearchResult> SearchUsersAsync(
        UserSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<UserDetails?> GetUserAsync(
    Guid externalId,
    CancellationToken cancellationToken = default);

    Task<UserAdministrationResult> SetActiveStatusAsync(
    Guid targetUserExternalId,
    bool isActive,
    Guid actingUserExternalId,
    CancellationToken cancellationToken = default);

    Task<UserAdministrationResult> SetAdministratorStatusAsync(
    Guid targetUserExternalId,
    bool isAdministrator,
    Guid actingUserExternalId,
    CancellationToken cancellationToken = default);
}
