using RTS.Domain.CandidateDiscovery;

namespace RTS.Infrastructure.CandidateDiscovery;

public sealed class DiscoveryRun
{
    public long Id { get; set; }
    public Guid ExternalId { get; set; }
    public long OwnerUserId { get; set; }
    public long ScreeningStrategyVersionId { get; set; }
    public string UniverseKey { get; set; } = string.Empty;
    public DateTime StartedUtc { get; set; }
    public DateTime CompletedUtc { get; set; }
    public string CriteriaSnapshot { get; set; } = string.Empty;
    public List<DiscoveryCandidate> Candidates { get; set; } = [];
}

public sealed class DiscoveryCandidate
{
    public long Id { get; set; }
    public Guid ExternalId { get; set; }
    public long DiscoveryRunId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public CandidateSourceType Source { get; set; }
    public ScreeningOutcome Outcome { get; set; }
    public decimal Score { get; set; }
    public int Rank { get; set; }
    public DateTime DataTimestampUtc { get; set; }
    public CandidateWorkflowStatus Status { get; set; }
    public string FactorsJson { get; set; } = "[]";
    public string EvidenceJson { get; set; } = "[]";
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
