namespace RTS.Infrastructure.Auditing;

public sealed class LoginHistory
{
    public long Id { get; set; }

    public Guid ExternalId { get; set; }

    public long? UserId { get; set; }

    public byte[]? IdentifierHash { get; set; }

    public string EventType { get; set; } = string.Empty;

    public bool IsSuccess { get; set; }

    public string? FailureCode { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public Guid? CorrelationId { get; set; }

    public DateTime CreatedUtc { get; set; }
}