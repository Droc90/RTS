namespace RTS.Contracts.Auditing;

public sealed record ApplicationErrorSearchRequest(
    string? SearchTerm = null,
    string? ResolutionStatus = null,
    int PageNumber = 1,
    int PageSize = 25);