namespace RTS.Contracts.Auditing;

public sealed record ApplicationErrorDetails(
    Guid ExternalId,
    Guid? CorrelationId,
    string ErrorType,
    string? ErrorCode,
    string? SafeMessage,
    string? DiagnosticDetails,
    string? Source,
    string? RequestPath,
    string? HttpMethod,
    int? StatusCode,
    string? UserEmail,
    DateTime OccurredUtc,
    string ResolutionStatus,
    DateTime? ResolvedUtc,
    string? ResolvedByUserEmail,
    string? ResolutionNotes,
    byte[] RowVersion);