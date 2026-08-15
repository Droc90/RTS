using RTS.Contracts.Auditing;

namespace RTS.Application.Auditing;

public interface IApplicationErrorAdministrationService
{
    Task<ApplicationErrorSearchResult> SearchAsync(
        ApplicationErrorSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationErrorDetails?> GetAsync(
        Guid externalId,
        CancellationToken cancellationToken = default);

    Task<UpdateApplicationErrorResult> UpdateAsync(
        Guid externalId,
        Guid actingUserExternalId,
        UpdateApplicationErrorRequest request,
        CancellationToken cancellationToken = default);
}