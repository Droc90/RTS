using RTS.Application.CandidateDiscovery;
using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.Charting;

public sealed record EvaluationResearchRequest(
    string Symbol,
    AssetType AssetType,
    DateTime AsOfUtc,
    CandidateEvaluationContext CandidateContext,
    ChartEvaluationSummary TechnicalSummary);

public sealed record EvaluationResearchResult(
    string Overview,
    IReadOnlyCollection<string> FundamentalFindings,
    IReadOnlyCollection<string> Catalysts,
    IReadOnlyCollection<string> Risks,
    IReadOnlyCollection<string> Uncertainties,
    IReadOnlyCollection<CandidateEvidence> Evidence,
    AiDiscoveryProvenance Provenance);

public interface IEvaluationResearchProvider
{
    Task<EvaluationResearchResult> ResearchAsync(EvaluationResearchRequest request,
        CancellationToken cancellationToken = default);
}
