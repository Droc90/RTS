namespace RTS.Contracts.Administration;

public sealed record UserSummary(
    Guid ExternalId,
    string Email,
    bool IsActive,
    bool IsDeleted,
    DateTime CreatedUtc,
    DateTime? LastLoginUtc,
    IReadOnlyCollection<string> Roles);