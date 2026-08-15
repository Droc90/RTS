namespace RTS.Application.Auditing;

public sealed record RecordLoginHistoryRequest(
    Guid? UserExternalId,
    string? Identifier,
    LoginEventType EventType,
    bool IsSuccess,
    LoginFailureCode? FailureCode,
    string? IpAddress,
    string? UserAgent,
    Guid? CorrelationId);