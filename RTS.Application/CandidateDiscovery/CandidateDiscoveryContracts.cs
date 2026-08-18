using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.CandidateDiscovery;

public sealed record CandidateEvidence(
    string Title,
    Uri Source,
    DateTime PublishedUtc,
    DateTime RetrievedUtc,
    EvidenceQuality Quality,
    string Summary);

public sealed record CandidateObservation(
    string Symbol,
    AssetType AssetType,
    DateTime DataTimestampUtc,
    IReadOnlyDictionary<string, object> Metrics,
    CandidateSourceType Source = CandidateSourceType.DeterministicScreen,
    IReadOnlyCollection<CandidateEvidence>? Evidence = null);

public sealed record RuleExplanation(
    string RuleName,
    string MetricKey,
    ScreeningOutcome Outcome,
    string Explanation,
    decimal ScoreContribution);

public sealed record CandidateScreeningResult(
    string Symbol,
    AssetType AssetType,
    CandidateSourceType Source,
    ScreeningOutcome Outcome,
    decimal Score,
    int Rank,
    DateTime DataTimestampUtc,
    CandidateWorkflowStatus Status,
    IReadOnlyCollection<RuleExplanation> Factors,
    IReadOnlyCollection<CandidateEvidence> Evidence);

public sealed record DiscoveryRunResult(
    Guid RunId,
    Guid StrategyVersionExternalId,
    int StrategyVersionNumber,
    string UniverseKey,
    DateTime StartedUtc,
    DateTime CompletedUtc,
    string CriteriaSnapshot,
    IReadOnlyCollection<CandidateScreeningResult> Candidates);

public interface ICatalystDiscoveryProvider
{
    Task<IReadOnlyCollection<CandidateObservation>> DiscoverAsync(
        CandidateUniverse universe,
        CancellationToken cancellationToken = default);
}

public interface ICatalystDiscoveryService
{
    Task<IReadOnlyCollection<CandidateObservation>> DiscoverAsync(
        CandidateUniverse universe,
        CancellationToken cancellationToken = default);
}
