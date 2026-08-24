using Microsoft.EntityFrameworkCore;
using RTS.Application.CandidateDiscovery;
using RTS.Application.EvaluationJobs;
using RTS.Domain.CandidateDiscovery;
using RTS.Infrastructure.Persistence;

namespace RTS.Infrastructure.EvaluationJobs;

public sealed class EvaluationJobService(RtsDbContext dbContext) : IEvaluationJobService
{
    public async Task<Guid> EnqueueSelectedCandidateAsync(Guid userExternalId, Guid candidateExternalId, bool forceNew = false, CancellationToken cancellationToken = default)
    {
        var ownerId = await GetOwnerIdAsync(userExternalId, cancellationToken);
        var candidate = await dbContext.DiscoveryCandidates.SingleOrDefaultAsync(item =>
            item.ExternalId == candidateExternalId &&
            dbContext.DiscoveryRuns.Any(run => run.Id == item.DiscoveryRunId && run.OwnerUserId == ownerId), cancellationToken)
            ?? throw new InvalidOperationException("The candidate could not be found.");

        if (!forceNew)
        {
            var existingJobExternalId = await dbContext.EvaluationJobs
                .Where(job => job.DiscoveryCandidateId == candidate.Id)
                .OrderByDescending(job => job.CreatedUtc)
                .Select(job => (Guid?)job.ExternalId)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingJobExternalId is not null) return existingJobExternalId.Value;
        }

        if (candidate.Status is CandidateWorkflowStatus.Proposed or CandidateWorkflowStatus.Deferred)
        {
            candidate.Status = CandidateWorkflowStatus.Selected;
            candidate.ModifiedUtc = DateTime.UtcNow;
        }
        else if (!CandidateWorkflow.IsApprovedForFullEvaluation(candidate.Status))
        {
            throw new InvalidOperationException("This candidate is not eligible for a full evaluation.");
        }

        var job = new EvaluationJob
        {
            ExternalId = Guid.NewGuid(), OwnerUserId = ownerId, DiscoveryCandidateId = candidate.Id,
            Symbol = candidate.Symbol, Status = EvaluationJobStatus.Queued, ProgressPercent = 0,
            ProgressMessage = "Waiting for an evaluation worker.", AttemptCount = 0, CreatedUtc = DateTime.UtcNow
        };
        dbContext.EvaluationJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job.ExternalId;
    }

    public async Task<IReadOnlyCollection<EvaluationJobDetails>> GetJobsAsync(Guid userExternalId, CancellationToken cancellationToken = default)
    {
        var ownerId = await GetOwnerIdAsync(userExternalId, cancellationToken);
        var cancelledArchiveCutoffUtc = DateTime.UtcNow - EvaluationJobRetentionPolicy.CancelledArchiveAfter;
        return await dbContext.EvaluationJobs.AsNoTracking().Where(job => job.OwnerUserId == ownerId)
            .Join(dbContext.DiscoveryCandidates, job => job.DiscoveryCandidateId, candidate => candidate.Id,
                (job, candidate) => new { Job = job, CandidateExternalId = candidate.ExternalId })
            .OrderByDescending(item => item.Job.CreatedUtc)
            .Select(item => new EvaluationJobDetails(
                item.Job.ExternalId, item.CandidateExternalId, item.Job.Symbol, item.Job.Status,
                item.Job.ProgressPercent, item.Job.ProgressMessage, item.Job.ErrorMessage,
                item.Job.AttemptCount, item.Job.CreatedUtc, item.Job.StartedUtc,
                item.Job.CompletedUtc,
                item.Job.Status == EvaluationJobStatus.Cancelled && item.Job.CompletedUtc < cancelledArchiveCutoffUtc,
                item.Job.RowVersion))
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> CancelAsync(Guid userExternalId, Guid jobExternalId, byte[] rowVersion, CancellationToken cancellationToken = default) =>
        ChangeAsync(userExternalId, jobExternalId, rowVersion, retry: false, cancellationToken);

    public Task<bool> RetryAsync(Guid userExternalId, Guid jobExternalId, byte[] rowVersion, CancellationToken cancellationToken = default) =>
        ChangeAsync(userExternalId, jobExternalId, rowVersion, retry: true, cancellationToken);

    private async Task<bool> ChangeAsync(Guid userExternalId, Guid externalId, byte[] rowVersion, bool retry, CancellationToken cancellationToken)
    {
        if (rowVersion.Length == 0) return false;
        var ownerId = await GetOwnerIdAsync(userExternalId, cancellationToken);
        var job = await dbContext.EvaluationJobs.SingleOrDefaultAsync(item => item.ExternalId == externalId && item.OwnerUserId == ownerId, cancellationToken);
        if (job is null || !job.RowVersion.SequenceEqual(rowVersion)) return false;
        if (retry && job.Status is not (EvaluationJobStatus.Failed or EvaluationJobStatus.Cancelled)) return false;
        if (!retry && job.Status is not (EvaluationJobStatus.Queued or EvaluationJobStatus.Running)) return false;

        job.Status = retry ? EvaluationJobStatus.Queued : EvaluationJobStatus.Cancelled;
        job.ProgressPercent = 0;
        job.ProgressMessage = retry ? "Queued for retry." : "Cancelled by the user.";
        job.ErrorMessage = null;
        job.StartedUtc = null;
        job.CompletedUtc = retry ? null : DateTime.UtcNow;
        dbContext.Entry(job).Property(item => item.RowVersion).OriginalValue = rowVersion;
        try { await dbContext.SaveChangesAsync(cancellationToken); return true; }
        catch (DbUpdateConcurrencyException) { return false; }
    }

    private async Task<long> GetOwnerIdAsync(Guid externalId, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(user => user.ExternalId == externalId && user.IsActive && !user.IsDeleted)
            .Select(user => (long?)user.Id).SingleOrDefaultAsync(cancellationToken)
        ?? throw new InvalidOperationException("The active user account could not be found.");

}
