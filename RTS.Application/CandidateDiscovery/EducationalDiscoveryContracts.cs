using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.CandidateDiscovery;

public sealed record EducationalDiscoveryRequest(
    IReadOnlyCollection<string> CurrentHoldings,
    IReadOnlyCollection<string> ExcludedSymbols,
    decimal? AccountValue,
    IReadOnlyCollection<string> PreferredThemes,
    string? DiversificationGoals,
    int MaximumCandidates = 8,
    CandidateIdentificationSettings? IdentificationSettings = null);

public sealed record EducationalCandidate(
    int Rank,
    string Symbol,
    AssetType AssetType,
    string TypeDescription,
    decimal CandidateQuality,
    string InitialStatus,
    string PrimaryThesis,
    string PortfolioFit,
    string KeyRisks,
    string EducationalContext,
    IReadOnlyCollection<CandidateEvidence> Evidence);

public sealed record EducationalDiscoveryReport(
    Guid DiscoveryRunExternalId,
    DateTime GeneratedUtc,
    string Summary,
    IReadOnlyCollection<string> RecommendedEvaluationOrder,
    IReadOnlyCollection<EducationalCandidate> Candidates);

public interface IEducationalCandidateDiscoveryProvider
{
    Task<EducationalDiscoveryReport> DiscoverAsync(
        EducationalDiscoveryRequest request,
        CancellationToken cancellationToken = default);
}
