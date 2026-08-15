namespace RTS.Infrastructure.Auditing;

public sealed class ApplicationError
{
    public long Id { get; set; }

    public Guid ExternalId { get; set; }

    public long? UserId { get; set; }

    public Guid? CorrelationId { get; set; }

    public string ErrorType { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public string? SafeMessage { get; set; }

    public string? DiagnosticDetails { get; set; }

    public string? Source { get; set; }

    public string? RequestPath { get; set; }

    public string? HttpMethod { get; set; }

    public int? StatusCode { get; set; }

    public DateTime OccurredUtc { get; set; }

    public string ResolutionStatus { get; set; } = "New";

    public DateTime? ResolvedUtc { get; set; }

    public long? ResolvedByUserId { get; set; }

    public string? ResolutionNotes { get; set; }

    public byte[] RowVersion { get; set; } = [];
}