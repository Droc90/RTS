namespace RTS.Application.Auditing;

public sealed record RecordAuditRequest(
    Guid? ActingUserExternalId,
    AuditAction Action,
    string EventCategory,
    string EntityType,
    Guid? EntityExternalId,
    IReadOnlyDictionary<string, object?>? OldValues,
    IReadOnlyDictionary<string, object?>? NewValues,
    bool IsSuccess,
    string? FailureReason = null,
    string? IpAddress = null,
    string? UserAgent = null,
    Guid? CorrelationId = null);