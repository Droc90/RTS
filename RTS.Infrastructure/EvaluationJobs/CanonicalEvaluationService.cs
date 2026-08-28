using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Charting;
using RTS.Application.CandidateDiscovery;
using RTS.Application.EvaluationJobs;
using RTS.Infrastructure.Persistence;
using RTS.Infrastructure.AiUsage;

namespace RTS.Infrastructure.EvaluationJobs;

public sealed class CanonicalEvaluationService(
    RtsDbContext dbContext,
    IFinancialChartService chartService,
    IChartEvaluationConfigurationProvider configurationProvider,
    IEvaluationResearchProvider researchProvider) : ICanonicalEvaluationService
{
    public const string CurrentCalculationVersion = "2.0";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CanonicalEvaluationResult> GenerateAsync(Guid evaluationJobExternalId,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindEntityAsync(evaluationJobExternalId, cancellationToken);
        if (existing is not null) return await DeserializeAsync(existing, cancellationToken);

        var job = await dbContext.EvaluationJobs.AsNoTracking()
            .Where(item => item.ExternalId == evaluationJobExternalId)
            .Select(item => new
            {
                item.Id,
                item.ExternalId,
                item.Symbol,
                item.OwnerUserId,
                item.CompletedUtc,
                Candidate = dbContext.DiscoveryCandidates.Where(candidate => candidate.Id == item.DiscoveryCandidateId)
                    .Select(candidate => new
                    {
                        candidate.AssetType, candidate.Source, candidate.Score, candidate.DataTimestampUtc,
                        candidate.FactorsJson, candidate.EvidenceJson, candidate.DiscoveryRunId
                    }).Single(),
                OwnerExternalId = dbContext.Users.Where(user => user.Id == item.OwnerUserId)
                    .Select(user => user.ExternalId).Single()
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The evaluation job could not be found.");

        var views = await chartService.GetViewsAsync(job.OwnerExternalId, job.ExternalId, cancellationToken);
        if (views.Count == 0)
            throw new InvalidOperationException("Market data must be prepared before an evaluation result can be generated.");

        var configuration = await configurationProvider.GetAsync(job.OwnerExternalId, cancellationToken);
        var summary = InitialChartEvaluator.Evaluate(views, configuration);
        var factors = JsonSerializer.Deserialize<RuleExplanation[]>(job.Candidate.FactorsJson, JsonOptions) ?? [];
        string? Factor(string metricKey) => factors.FirstOrDefault(factor =>
            string.Equals(factor.MetricKey, metricKey, StringComparison.OrdinalIgnoreCase))?.Explanation;
        var criteriaSnapshot = await dbContext.DiscoveryRuns.AsNoTracking()
            .Where(run => run.Id == job.Candidate.DiscoveryRunId)
            .Select(run => run.CriteriaSnapshot)
            .SingleAsync(cancellationToken);
        var context = new CandidateEvaluationContext(
            job.Candidate.AssetType, job.Candidate.Source, job.Candidate.Score,
            job.Candidate.DataTimestampUtc, Factor("AI.PrimaryThesis"), Factor("AI.PortfolioFit"),
            Factor("AI.KeyRisks"), Factor("AI.EducationalContext"),
            JsonSerializer.Deserialize<CandidateEvidence[]>(job.Candidate.EvidenceJson, JsonOptions) ?? [],
            criteriaSnapshot, ReadAiProvenance(criteriaSnapshot));
        var research = await researchProvider.ResearchAsync(new EvaluationResearchRequest(
            job.Symbol, job.Candidate.AssetType, DateTime.UtcNow, context, summary), cancellationToken);
        if (research.Provenance.Usage is { } usage)
        {
            dbContext.AiUsageRecords.Add(new AiUsageRecord
            {
                OwnerUserId = job.OwnerUserId,
                OperationExternalId = job.ExternalId,
                OperationType = "FullEvaluationResearch",
                Provider = research.Provenance.Provider,
                Model = research.Provenance.Model,
                PromptVersion = research.Provenance.PromptVersion,
                SchemaVersion = research.Provenance.SchemaVersion,
                InputTokens = usage.InputTokens,
                CachedInputTokens = usage.CachedInputTokens,
                OutputTokens = usage.OutputTokens,
                ReasoningOutputTokens = usage.ReasoningOutputTokens,
                TotalTokens = usage.TotalTokens,
                WebSearchCalls = usage.WebSearchCalls,
                RecordedUtc = DateTime.UtcNow
            });
        }
        var snapshots = await dbContext.MarketDataSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.EvaluationJobId == job.Id)
            .OrderBy(snapshot => snapshot.Id)
            .Select(snapshot => new
            {
                snapshot.ExternalId, snapshot.ProviderKey, snapshot.RetrievedUtc,
                snapshot.RequestJson, snapshot.MissingBarsJson
            })
            .ToArrayAsync(cancellationToken);
        var provenance = snapshots.Select(snapshot =>
        {
            var request = JsonSerializer.Deserialize<RTS.Application.MarketData.MarketDataRequest>(
                snapshot.RequestJson, JsonOptions)
                ?? throw new InvalidOperationException("A saved market-data request is invalid.");
            var missing = JsonSerializer.Deserialize<DateTime[]>(snapshot.MissingBarsJson, JsonOptions) ?? [];
            return new EvaluationMarketDataProvenance(snapshot.ExternalId, snapshot.ProviderKey,
                snapshot.RetrievedUtc, request.FromUtc, request.ToUtc, request.IntervalValue,
                request.IntervalUnit, missing.Length);
        }).ToArray();
        var snapshotIds = provenance.Select(item => item.SnapshotExternalId).ToArray();
        // For legacy backfill, retain the original completion time as the evaluation timestamp.
        var createdUtc = job.CompletedUtc ?? DateTime.UtcNow;
        var entity = new EvaluationResult
        {
            ExternalId = Guid.NewGuid(),
            EvaluationJobId = job.Id,
            CreatedUtc = createdUtc,
            CalculationVersion = CurrentCalculationVersion,
            ConfigurationJson = JsonSerializer.Serialize(configuration, JsonOptions),
            ResultJson = JsonSerializer.Serialize(new StoredEvaluationPayload(summary, context, provenance, research), JsonOptions),
            MarketDataSnapshotExternalIdsJson = JsonSerializer.Serialize(snapshotIds, JsonOptions)
        };
        dbContext.EvaluationResults.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(entity.ExternalId, job.ExternalId, job.Symbol, createdUtc,
            entity.CalculationVersion, configuration, summary, snapshotIds, context, provenance, research);
    }

    public async Task<CanonicalEvaluationResult?> GetOrGenerateAsync(Guid userExternalId,
        Guid evaluationJobExternalId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.EvaluationJobs.AsNoTracking()
            .Where(item => item.ExternalId == evaluationJobExternalId &&
                dbContext.Users.Any(user => user.Id == item.OwnerUserId && user.ExternalId == userExternalId &&
                    user.IsActive && !user.IsDeleted))
            .Select(item => new { item.Status })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The evaluation job could not be found.");
        var existing = await FindEntityAsync(evaluationJobExternalId, cancellationToken);
        if (existing is not null) return await DeserializeAsync(existing, cancellationToken);
        return job.Status == EvaluationJobStatus.Succeeded
            ? await GenerateAsync(evaluationJobExternalId, cancellationToken)
            : null;
    }

    public async Task<EvaluationChangeSummary?> GetChangesAsync(Guid userExternalId,
        Guid evaluationJobExternalId, CancellationToken cancellationToken = default)
    {
        var currentJob = await dbContext.EvaluationJobs.AsNoTracking()
            .Where(job => job.ExternalId == evaluationJobExternalId &&
                dbContext.Users.Any(user => user.Id == job.OwnerUserId && user.ExternalId == userExternalId &&
                    user.IsActive && !user.IsDeleted))
            .Select(job => new { job.Id, job.OwnerUserId, job.Symbol, job.CreatedUtc })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The evaluation job could not be found.");
        var previousJobExternalId = await dbContext.EvaluationJobs.AsNoTracking()
            .Where(job => job.OwnerUserId == currentJob.OwnerUserId && job.Symbol == currentJob.Symbol &&
                job.Id != currentJob.Id && job.Status == EvaluationJobStatus.Succeeded &&
                job.CreatedUtc < currentJob.CreatedUtc)
            .OrderByDescending(job => job.CreatedUtc)
            .Select(job => (Guid?)job.ExternalId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!previousJobExternalId.HasValue) return null;

        var current = await GetOrGenerateAsync(userExternalId, evaluationJobExternalId, cancellationToken);
        var previous = await GetOrGenerateAsync(userExternalId, previousJobExternalId.Value, cancellationToken);
        if (current is null || previous is null) return null;
        var timeframeChanges = current.Summary.Timeframes
            .Select(timeframe =>
            {
                var prior = previous.Summary.Timeframes.FirstOrDefault(item =>
                    string.Equals(item.TimeframeKey, timeframe.TimeframeKey, StringComparison.OrdinalIgnoreCase));
                if (prior is null) return $"{timeframe.TimeframeName} was added at {timeframe.Score:0.0}.";
                var delta = timeframe.Score - prior.Score;
                return delta == 0 && timeframe.Tone == prior.Tone &&
                    timeframe.PassedMandatoryGates == prior.PassedMandatoryGates
                    ? null
                    : $"{timeframe.TimeframeName}: {prior.Score:0.0} to {timeframe.Score:0.0} " +
                      $"({FormatDelta(delta)}); {ToneName(prior.Tone)} to {ToneName(timeframe.Tone)}.";
            })
            .Where(change => change is not null)
            .Cast<string>()
            .ToArray();
        return new(previous.EvaluationJobExternalId, previous.CreatedUtc,
            previous.Summary.OverallScore, current.Summary.OverallScore,
            current.Summary.OverallScore - previous.Summary.OverallScore,
            previous.Summary.Tone, current.Summary.Tone,
            previous.Summary.PassedMandatoryGates != current.Summary.PassedMandatoryGates,
            previous.Configuration.Name != current.Configuration.Name ||
                previous.Configuration.Version != current.Configuration.Version,
            previous.CalculationVersion != current.CalculationVersion,
            timeframeChanges);
    }

    private static string FormatDelta(decimal value) => value > 0 ? $"+{value:0.0}" : value.ToString("0.0");
    private static string ToneName(TechnicalFindingTone tone) => tone.ToString().ToLowerInvariant();

    private Task<EvaluationResult?> FindEntityAsync(Guid jobExternalId, CancellationToken cancellationToken) =>
        dbContext.EvaluationResults.AsNoTracking()
            .Where(result => dbContext.EvaluationJobs.Any(job => job.Id == result.EvaluationJobId && job.ExternalId == jobExternalId))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<CanonicalEvaluationResult> DeserializeAsync(EvaluationResult entity,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.EvaluationJobs.AsNoTracking()
            .Where(item => item.Id == entity.EvaluationJobId)
            .Select(item => new { item.ExternalId, item.Symbol })
            .SingleAsync(cancellationToken);
        var payload = DeserializePayload(entity.ResultJson);
        return new(entity.ExternalId, job.ExternalId, job.Symbol, entity.CreatedUtc,
            entity.CalculationVersion,
            JsonSerializer.Deserialize<ChartEvaluationConfiguration>(entity.ConfigurationJson, JsonOptions)
                ?? throw new InvalidOperationException("The saved evaluation configuration is invalid."),
            payload.Summary,
            JsonSerializer.Deserialize<Guid[]>(entity.MarketDataSnapshotExternalIdsJson, JsonOptions) ?? [],
            payload.CandidateContext,
            payload.MarketDataProvenance,
            payload.Research);
    }

    private static StoredEvaluationPayload DeserializePayload(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("summary", out _))
            return JsonSerializer.Deserialize<StoredEvaluationPayload>(json, JsonOptions)
                ?? throw new InvalidOperationException("The saved evaluation result is invalid.");
        var summary = JsonSerializer.Deserialize<ChartEvaluationSummary>(json, JsonOptions)
            ?? throw new InvalidOperationException("The saved evaluation result is invalid.");
        return new(summary, null, null, null);
    }

    private static AiDiscoveryProvenance? ReadAiProvenance(string criteriaSnapshot)
    {
        try
        {
            using var document = JsonDocument.Parse(criteriaSnapshot);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty("AiProvenance", out var provenance) &&
                !root.TryGetProperty("aiProvenance", out provenance)) return null;
            return provenance.ValueKind == JsonValueKind.Null
                ? null
                : provenance.Deserialize<AiDiscoveryProvenance>(JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record StoredEvaluationPayload(
        ChartEvaluationSummary Summary,
        CandidateEvaluationContext? CandidateContext,
        IReadOnlyCollection<EvaluationMarketDataProvenance>? MarketDataProvenance,
        EvaluationResearchResult? Research);
}
