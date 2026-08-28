namespace RTS.Application.Charting;

using RTS.Application.CandidateDiscovery;
using RTS.Domain.CandidateDiscovery;
using RTS.Domain.TradingModels.Timeframes;

public sealed record CandidateEvaluationContext(
    AssetType AssetType,
    CandidateSourceType Source,
    decimal DiscoveryScore,
    DateTime DiscoveryDataTimestampUtc,
    string? PrimaryThesis,
    string? PortfolioFit,
    string? IdentifiedRisks,
    string? EvaluationFocus,
    IReadOnlyCollection<CandidateEvidence> Evidence,
    string CriteriaSnapshot,
    AiDiscoveryProvenance? AiProvenance = null);

public sealed record EvaluationMarketDataProvenance(
    Guid SnapshotExternalId,
    string ProviderKey,
    DateTime RetrievedUtc,
    DateTime RequestedFromUtc,
    DateTime RequestedToUtc,
    int IntervalValue,
    BarIntervalUnit IntervalUnit,
    int MissingBarCount);

public sealed record CanonicalEvaluationResult(
    Guid ExternalId,
    Guid EvaluationJobExternalId,
    string Symbol,
    DateTime CreatedUtc,
    string CalculationVersion,
    ChartEvaluationConfiguration Configuration,
    ChartEvaluationSummary Summary,
    IReadOnlyCollection<Guid> MarketDataSnapshotExternalIds,
    CandidateEvaluationContext? CandidateContext = null,
    IReadOnlyCollection<EvaluationMarketDataProvenance>? MarketDataProvenance = null,
    EvaluationResearchResult? Research = null);

public sealed record EvaluationChangeSummary(
    Guid PreviousEvaluationJobExternalId,
    DateTime PreviousEvaluationUtc,
    decimal PreviousScore,
    decimal CurrentScore,
    decimal ScoreChange,
    TechnicalFindingTone PreviousTone,
    TechnicalFindingTone CurrentTone,
    bool GateStatusChanged,
    bool ConfigurationChanged,
    bool CalculationVersionChanged,
    IReadOnlyCollection<string> TimeframeChanges);

public interface ICanonicalEvaluationService
{
    Task<CanonicalEvaluationResult> GenerateAsync(Guid evaluationJobExternalId,
        CancellationToken cancellationToken = default);

    Task<CanonicalEvaluationResult?> GetOrGenerateAsync(Guid userExternalId,
        Guid evaluationJobExternalId,
        CancellationToken cancellationToken = default);

    Task<EvaluationChangeSummary?> GetChangesAsync(Guid userExternalId,
        Guid evaluationJobExternalId,
        CancellationToken cancellationToken = default);
}
