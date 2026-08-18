using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RTS.Application.CandidateDiscovery;
using RTS.Domain.CandidateDiscovery;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.CandidateDiscovery;

public sealed class CandidateInboxService(RtsDbContext dbContext) : ICandidateInboxService
{
    public async Task<Guid> SaveRunAsync(Guid userExternalId, DiscoveryRunResult run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        var ownerId = await GetActiveUserIdAsync(userExternalId, cancellationToken);
        var strategyVersionId = await dbContext.ScreeningStrategyVersions
            .Where(version => version.ExternalId == run.StrategyVersionExternalId)
            .Select(version => (long?)version.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The screening-strategy version could not be found.");

        if (await dbContext.DiscoveryRuns.AnyAsync(candidate => candidate.ExternalId == run.RunId, cancellationToken))
            throw new InvalidOperationException("The discovery run has already been saved.");

        var entity = new DiscoveryRun
        {
            ExternalId = run.RunId,
            OwnerUserId = ownerId,
            ScreeningStrategyVersionId = strategyVersionId,
            UniverseKey = run.UniverseKey,
            StartedUtc = run.StartedUtc,
            CompletedUtc = run.CompletedUtc,
            CriteriaSnapshot = run.CriteriaSnapshot,
            Candidates = run.Candidates.Select(candidate => new DiscoveryCandidate
            {
                ExternalId = Guid.NewGuid(),
                Symbol = candidate.Symbol,
                AssetType = candidate.AssetType,
                Source = candidate.Source,
                Outcome = candidate.Outcome,
                Score = candidate.Score,
                Rank = candidate.Rank,
                DataTimestampUtc = candidate.DataTimestampUtc,
                Status = candidate.Status,
                FactorsJson = JsonSerializer.Serialize(candidate.Factors),
                EvidenceJson = JsonSerializer.Serialize(candidate.Evidence),
                CreatedUtc = DateTime.UtcNow
            }).ToList()
        };

        dbContext.DiscoveryRuns.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ExternalId;
    }

    public async Task<IReadOnlyCollection<CandidateInboxItem>> GetInboxAsync(Guid userExternalId, CancellationToken cancellationToken = default)
    {
        var ownerId = await GetActiveUserIdAsync(userExternalId, cancellationToken);
        return await dbContext.DiscoveryCandidates.AsNoTracking()
            .Join(dbContext.DiscoveryRuns.Where(run => run.OwnerUserId == ownerId), candidate => candidate.DiscoveryRunId, run => run.Id,
                (candidate, run) => new CandidateInboxItem(candidate.ExternalId, run.ExternalId, candidate.Symbol, candidate.AssetType, candidate.Source, candidate.Outcome, candidate.Score, candidate.Rank, candidate.DataTimestampUtc, candidate.Status, candidate.FactorsJson, candidate.EvidenceJson, candidate.RowVersion))
            .OrderBy(candidate => candidate.Status)
            .ThenBy(candidate => candidate.Rank)
            .ThenBy(candidate => candidate.Symbol)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> SetStatusAsync(Guid userExternalId, Guid candidateExternalId, CandidateWorkflowStatus status, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (rowVersion.Length == 0) return false;
        var ownerId = await GetActiveUserIdAsync(userExternalId, cancellationToken);
        var candidate = await dbContext.DiscoveryCandidates
            .SingleOrDefaultAsync(item => item.ExternalId == candidateExternalId && dbContext.DiscoveryRuns.Any(run => run.Id == item.DiscoveryRunId && run.OwnerUserId == ownerId), cancellationToken);
        if (candidate is null || !candidate.RowVersion.SequenceEqual(rowVersion) || !CandidateWorkflow.CanTransition(candidate.Status, status)) return false;

        candidate.Status = status;
        candidate.ModifiedUtc = DateTime.UtcNow;
        dbContext.Entry(candidate).Property(item => item.RowVersion).OriginalValue = rowVersion;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private async Task<long> GetActiveUserIdAsync(Guid externalId, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(user => user.ExternalId == externalId && user.IsActive && !user.IsDeleted).Select(user => (long?)user.Id).SingleOrDefaultAsync(cancellationToken)
        ?? throw new InvalidOperationException("The active user account could not be found.");

}
