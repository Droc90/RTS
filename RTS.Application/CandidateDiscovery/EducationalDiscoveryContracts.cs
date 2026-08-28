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
    IReadOnlyCollection<EducationalCandidate> Candidates,
    AiDiscoveryProvenance? Provenance = null);

public sealed record AiDiscoveryProvenance(
    string Provider,
    string Model,
    string PromptVersion,
    string SchemaVersion,
    AiUsageMetrics? Usage = null);

public sealed record AiUsageMetrics(
    int InputTokens,
    int CachedInputTokens,
    int OutputTokens,
    int ReasoningOutputTokens,
    int TotalTokens,
    int WebSearchCalls);

public interface IEducationalCandidateDiscoveryProvider
{
    Task<EducationalDiscoveryReport> DiscoverAsync(
        EducationalDiscoveryRequest request,
        CancellationToken cancellationToken = default);
}
