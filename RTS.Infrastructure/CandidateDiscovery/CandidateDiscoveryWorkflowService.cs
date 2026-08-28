using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RTS.Application.CandidateDiscovery;
using RTS.Domain.CandidateDiscovery;
using RTS.Domain.ScreeningStrategies;
using RTS.Infrastructure.Persistence;
using RTS.Infrastructure.AiUsage;

namespace RTS.Infrastructure.CandidateDiscovery;

public sealed class CandidateDiscoveryWorkflowService(
    RtsDbContext dbContext,
    ICandidateDiscoveryService discoveryService,
    ICandidateInboxService inboxService,
    ICandidateIdentificationSettingsService settingsService,
    IEducationalCandidateDiscoveryProvider educationalProvider) : ICandidateDiscoveryWorkflowService
{
    private const string ManualStrategyName = "RTS Manual Candidate Entry";
    private const string InitialStrategyName = "RTS Initial Candidate Screen";

    public async Task<Guid> AddManualAsync(Guid userExternalId, string symbol, AssetType assetType, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;
        var observation = discoveryService.CreateManual(symbol, assetType, timestamp);
        return await SaveAsync(userExternalId, await GetManualStrategyAsync(cancellationToken), [observation], "manual", timestamp, cancellationToken);
    }

    public async Task<Guid> AddWatchlistAsync(Guid userExternalId, IEnumerable<string> symbols, AssetType assetType, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;
        var observations = discoveryService.CreateWatchlist(symbols, assetType, timestamp);
        if (observations.Count == 0) throw new ArgumentException("At least one watchlist symbol is required.", nameof(symbols));
        return await SaveAsync(userExternalId, await GetManualStrategyAsync(cancellationToken), observations, "watchlist", timestamp, cancellationToken);
    }

    public async Task<Guid> RunDeterministicScreenAsync(Guid userExternalId, IEnumerable<DeterministicCandidateInput> candidates, CancellationToken cancellationToken = default)
    {
        var inputs = candidates.ToArray();
        if (inputs.Length == 0) throw new ArgumentException("At least one candidate is required.", nameof(candidates));
        var timestamp = DateTime.UtcNow;
        var observations = inputs.Select(input => new CandidateObservation(
            input.Symbol, input.AssetType, timestamp,
            new Dictionary<string, object>
            {
                ["Market.Price"] = input.Price,
                ["Market.AverageDailyDollarVolume"] = input.AverageDailyDollarVolume,
                ["Fundamental.RevenueGrowth"] = input.RevenueGrowthPercent,
                ["Fundamental.EarningsGrowth"] = input.EarningsGrowthPercent,
                ["Fundamental.ForwardPriceToEarningsRatio"] = input.ForwardPriceToEarningsRatio
            })).ToArray();
        var strategy = await GetInitialStrategyAsync(cancellationToken);
        return await SaveAsync(userExternalId, strategy, observations, "deterministic-entry", timestamp, cancellationToken);
    }

    public async Task<EducationalDiscoveryReport> DiscoverEducationalCandidatesAsync(Guid userExternalId, EducationalDiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MaximumCandidates is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(request), "Maximum candidates must be between 1 and 20.");
        var settings = await settingsService.GetOrCreateAsync(userExternalId, cancellationToken);
        var ownerId = await dbContext.Users.Where(user => user.ExternalId == userExternalId && user.IsActive && !user.IsDeleted)
            .Select(user => (long?)user.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The active user account could not be found.");
        var historicalExclusions = await dbContext.DiscoveryCandidates.AsNoTracking()
            .Where(candidate => dbContext.DiscoveryRuns.Any(run => run.Id == candidate.DiscoveryRunId && run.OwnerUserId == ownerId) &&
                ((!settings.Settings.IncludeRejected && candidate.Status == CandidateWorkflowStatus.Rejected) ||
                 (!settings.Settings.IncludePreviouslyReviewed && dbContext.EvaluationJobs.Any(job => job.DiscoveryCandidateId == candidate.Id))))
            .Select(candidate => candidate.Symbol)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        request = request with
        {
            IdentificationSettings = settings.Settings,
            ExcludedSymbols = request.ExcludedSymbols.Concat(historicalExclusions).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
        var report = await educationalProvider.DiscoverAsync(request, cancellationToken);
        if (report.Provenance?.Usage is { } usage)
        {
            dbContext.AiUsageRecords.Add(new AiUsageRecord
            {
                OwnerUserId = ownerId,
                OperationExternalId = report.DiscoveryRunExternalId,
                OperationType = "CandidateDiscovery",
                Provider = report.Provenance.Provider,
                Model = report.Provenance.Model,
                PromptVersion = report.Provenance.PromptVersion,
                SchemaVersion = report.Provenance.SchemaVersion,
                InputTokens = usage.InputTokens,
                CachedInputTokens = usage.CachedInputTokens,
                OutputTokens = usage.OutputTokens,
                ReasoningOutputTokens = usage.ReasoningOutputTokens,
                TotalTokens = usage.TotalTokens,
                WebSearchCalls = usage.WebSearchCalls,
                RecordedUtc = DateTime.UtcNow
            });
        }
        var strategy = await GetManualStrategyAsync(cancellationToken);
        var version = strategy.Versions.Single(item => item.Status == ScreeningStrategyVersionStatus.Published);
        var candidates = report.Candidates.Select(candidate => new CandidateScreeningResult(
            candidate.Symbol, candidate.AssetType, CandidateSourceType.AiCatalyst,
            candidate.CandidateQuality >= 80 ? ScreeningOutcome.Passed : candidate.CandidateQuality >= 60 ? ScreeningOutcome.Warning : ScreeningOutcome.Failed,
            candidate.CandidateQuality, candidate.Rank, report.GeneratedUtc, CandidateWorkflowStatus.Proposed,
            new RuleExplanation[]
            {
                new("Type of business", "AI.TypeDescription", ScreeningOutcome.Passed, candidate.TypeDescription, 0),
                new("Candidate quality", "AI.CandidateQuality", ScreeningOutcome.Passed, $"Candidate quality {candidate.CandidateQuality:0.#}/100. Status: {candidate.InitialStatus}.", candidate.CandidateQuality),
                new("Primary thesis", "AI.PrimaryThesis", ScreeningOutcome.Passed, candidate.PrimaryThesis, 0),
                new("Portfolio fit", "AI.PortfolioFit", ScreeningOutcome.Passed, candidate.PortfolioFit, 0),
                new("Key risks", "AI.KeyRisks", ScreeningOutcome.Warning, candidate.KeyRisks, 0),
                new("What evaluation will inspect", "AI.EducationalContext", ScreeningOutcome.Passed, candidate.EducationalContext, 0)
            }, candidate.Evidence)).ToArray();
        var criteriaSnapshot = JsonSerializer.Serialize(new
        {
            Request = request,
            AiProvenance = report.Provenance
        });
        var run = new DiscoveryRunResult(report.DiscoveryRunExternalId, version.ExternalId, version.VersionNumber,
            "ai-educational-discovery", report.GeneratedUtc, DateTime.UtcNow, criteriaSnapshot, candidates);
        await inboxService.SaveRunAsync(userExternalId, run, cancellationToken);
        return report;
    }

    private async Task<Guid> SaveAsync(Guid userExternalId, ScreeningStrategy strategy, IReadOnlyCollection<CandidateObservation> observations, string universeKey, DateTime timestamp, CancellationToken cancellationToken)
    {
        var version = strategy.Versions.Single(item => item.Status == ScreeningStrategyVersionStatus.Published);
        var universe = CandidateUniverse.Create(universeKey, universeKey, observations.Select(item => item.AssetType), observations.Select(item => item.Symbol));
        var run = discoveryService.Run(universe, version, observations, startedUtc: timestamp);
        return await inboxService.SaveRunAsync(userExternalId, run, cancellationToken);
    }

    private Task<ScreeningStrategy> GetManualStrategyAsync(CancellationToken cancellationToken) => GetOrCreateAsync(ManualStrategyName, false, cancellationToken);
    private Task<ScreeningStrategy> GetInitialStrategyAsync(CancellationToken cancellationToken) => GetOrCreateAsync(InitialStrategyName, true, cancellationToken);

    private async Task<ScreeningStrategy> GetOrCreateAsync(string name, bool initial, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ScreeningStrategies.AsSplitQuery().Include(strategy => strategy.Versions)
            .ThenInclude(version => version.Rules).SingleOrDefaultAsync(strategy => strategy.OwnerUserId == null && strategy.Name == name, cancellationToken);
        if (existing is not null) return existing;

        var strategy = initial
            ? InitialCandidateMethodology.Create(DateTime.UtcNow)
            : CreateManualStrategy();
        dbContext.ScreeningStrategies.Add(strategy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return strategy;
    }

    private static ScreeningStrategy CreateManualStrategy()
    {
        var strategy = ScreeningStrategy.Create(ManualStrategyName, "Accepts user-entered candidates without applying deterministic filters.", null);
        var draft = strategy.Versions.Single();
        strategy.PublishDraftVersion(draft.ExternalId, draft.CreatedUtc);
        return strategy;
    }
}
