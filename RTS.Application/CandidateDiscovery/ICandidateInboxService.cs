using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.CandidateDiscovery;

public sealed record CandidateInboxItem(
    Guid ExternalId,
    Guid RunExternalId,
    string Symbol,
    AssetType AssetType,
    CandidateSourceType Source,
    ScreeningOutcome Outcome,
    decimal Score,
    int Rank,
    DateTime DataTimestampUtc,
    CandidateWorkflowStatus Status,
    string FactorsJson,
    string EvidenceJson,
    byte[] RowVersion);

public interface ICandidateInboxService
{
    Task<Guid> SaveRunAsync(
        Guid userExternalId,
        DiscoveryRunResult run,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CandidateInboxItem>> GetInboxAsync(
        Guid userExternalId,
        CancellationToken cancellationToken = default);

    Task<bool> SetStatusAsync(
        Guid userExternalId,
        Guid candidateExternalId,
        CandidateWorkflowStatus status,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);
}
