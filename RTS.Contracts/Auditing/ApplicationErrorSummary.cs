namespace RTS.Contracts.Auditing;

public sealed record ApplicationErrorSummary(
    Guid ExternalId,
    Guid? CorrelationId,
    string ErrorType,
    string? ErrorCode,
    string? SafeMessage,
    string? RequestPath,
    string? HttpMethod,
    int? StatusCode,
    string? UserEmail,
    DateTime OccurredUtc,
    string ResolutionStatus);