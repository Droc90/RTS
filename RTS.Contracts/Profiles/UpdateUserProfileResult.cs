namespace RTS.Contracts.Profiles;

public sealed record UpdateUserProfileResult(
    bool Succeeded,
    UserProfileDetails? Profile,
    string? ErrorCode)
{
    public static UpdateUserProfileResult Success(
        UserProfileDetails profile) =>
        new(
            true,
            profile,
            null);

    public static UpdateUserProfileResult Failure(
        string errorCode) =>
        new(
            false,
            null,
            errorCode);
}