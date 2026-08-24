using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.CandidateDiscovery;

public sealed record DeterministicCandidateInput(
    string Symbol,
    AssetType AssetType,
    decimal Price,
    decimal AverageDailyDollarVolume,
    decimal RevenueGrowthPercent,
    decimal EarningsGrowthPercent,
    decimal ForwardPriceToEarningsRatio);

public interface ICandidateDiscoveryWorkflowService
{
    Task<Guid> AddManualAsync(Guid userExternalId, string symbol, AssetType assetType, CancellationToken cancellationToken = default);
    Task<Guid> AddWatchlistAsync(Guid userExternalId, IEnumerable<string> symbols, AssetType assetType, CancellationToken cancellationToken = default);
    Task<Guid> RunDeterministicScreenAsync(Guid userExternalId, IEnumerable<DeterministicCandidateInput> candidates, CancellationToken cancellationToken = default);
    Task<EducationalDiscoveryReport> DiscoverEducationalCandidatesAsync(Guid userExternalId, EducationalDiscoveryRequest request, CancellationToken cancellationToken = default);
}
