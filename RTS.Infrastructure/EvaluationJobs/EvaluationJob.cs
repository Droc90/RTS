using RTS.Application.EvaluationJobs;

namespace RTS.Infrastructure.EvaluationJobs;

public sealed class EvaluationJob
{
    public long Id { get; set; }
    public Guid ExternalId { get; set; }
    public long OwnerUserId { get; set; }
    public long DiscoveryCandidateId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public EvaluationJobStatus Status { get; set; }
    public int ProgressPercent { get; set; }
    public string? ProgressMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? StartedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class MarketDataSnapshot
{
    public long Id { get; set; }
    public Guid ExternalId { get; set; }
    public long EvaluationJobId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public DateTime RetrievedUtc { get; set; }
    public string RequestJson { get; set; } = string.Empty;
    public string BarsJson { get; set; } = string.Empty;
    public string MissingBarsJson { get; set; } = string.Empty;
}

public sealed class EvaluationResult
{
    public long Id { get; set; }
    public Guid ExternalId { get; set; }
    public long EvaluationJobId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string CalculationVersion { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = string.Empty;
    public string ResultJson { get; set; } = string.Empty;
    public string MarketDataSnapshotExternalIdsJson { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}
