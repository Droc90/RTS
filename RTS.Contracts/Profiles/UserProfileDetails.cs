namespace RTS.Contracts.Profiles;

public sealed record UserProfileDetails(
    Guid ExternalId,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    string? TimeZoneId,
    string? Locale,
    string? PreferredDateFormat,
    bool IsOnboardingComplete,
    byte[] RowVersion);