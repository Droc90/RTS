namespace RTS.Application.EvaluationJobs;

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
    byte[] RowVersion);

public interface IEvaluationJobService
{
    Task<Guid> EnqueueSelectedCandidateAsync(Guid userExternalId, Guid candidateExternalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EvaluationJobDetails>> GetJobsAsync(Guid userExternalId, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid userExternalId, Guid jobExternalId, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<bool> RetryAsync(Guid userExternalId, Guid jobExternalId, byte[] rowVersion, CancellationToken cancellationToken = default);
}
