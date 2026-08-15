namespace RTS.Application.Auditing;

public sealed record RecordApplicationErrorRequest(
    Guid? UserExternalId,
    Guid CorrelationId,
    string ErrorType,
    string? ErrorCode,
    string SafeMessage,
    string? DiagnosticDetails,
    string? Source,
    string? RequestPath,
    string? HttpMethod,
    int? StatusCode);