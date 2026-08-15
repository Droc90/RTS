namespace RTS.Contracts.Administration;

public sealed record UserDetails(
    Guid ExternalId,
    string Email,
    string UserName,
    bool EmailConfirmed,
    string? PhoneNumber,
    bool PhoneNumberConfirmed,
    bool TwoFactorEnabled,
    DateTimeOffset? LockoutEnd,
    bool LockoutEnabled,
    int AccessFailedCount,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime? ModifiedUtc,
    DateTime? LastLoginUtc,
    bool IsDeleted,
    DateTime? DeletedUtc,
    IReadOnlyCollection<string> Roles);