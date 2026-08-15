namespace RTS.Contracts.Administration;

public sealed record UserSearchRequest(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 25);