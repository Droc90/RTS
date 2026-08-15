namespace RTS.Infrastructure.Auditing;

public sealed class AuditLog
{
    public long Id { get; set; }

    public Guid ExternalId { get; set; }

    public long? UserId { get; set; }

    public string? EventCategory { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public long? EntityId { get; set; }

    public Guid? EntityExternalId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public Guid? CorrelationId { get; set; }

    public bool IsSuccess { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedUtc { get; set; }
}