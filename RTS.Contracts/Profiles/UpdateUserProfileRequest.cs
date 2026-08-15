namespace RTS.Contracts.Profiles;

public sealed record UpdateUserProfileRequest(
    string? DisplayName,
    string? FirstName,
    string? LastName,
    string? TimeZoneId,
    string? Locale,
    string? PreferredDateFormat,
    byte[] RowVersion);