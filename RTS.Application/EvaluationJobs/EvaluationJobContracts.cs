namespace RTS.Application.EvaluationJobs;

public static class EvaluationJobRetentionPolicy
{
    public static readonly TimeSpan CancelledArchiveAfter = TimeSpan.FromDays(5);
    public static readonly TimeSpan CancelledDeleteAfter = TimeSpan.FromDays(30);
}

public enum EvaluationJobStatus
{
    Queued = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5
}

public sealed record EvaluationJobDetails(
    Guid ExternalId,
    Guid CandidateExternalId,
    string Symbol,
    EvaluationJobStatus Status,
    int ProgressPercent,
    string? ProgressMessage,
    string? ErrorMessage,
    int AttemptCount,
    DateTime CreatedUtc,
    DateTime? StartedUtc,
    DateTime? CompletedUtc,
    bool IsArchived,
    byte[] RowVersion);

public interface IEvaluationJobService
{
    Task<Guid> EnqueueSelectedCandidateAsync(Guid userExternalId, Guid candidateExternalId, bool forceNew = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EvaluationJobDetails>> GetJobsAsync(Guid userExternalId, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid userExternalId, Guid jobExternalId, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<bool> RetryAsync(Guid userExternalId, Guid jobExternalId, byte[] rowVersion, CancellationToken cancellationToken = default);
}

public interface IEvaluationJobProcessor
{
    Task ProcessAsync(Guid jobExternalId, string symbol, CancellationToken cancellationToken = default);
}
