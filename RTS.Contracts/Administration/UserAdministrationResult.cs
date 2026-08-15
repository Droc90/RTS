namespace RTS.Contracts.Administration;

public sealed record UserAdministrationResult(
    bool Succeeded,
    IReadOnlyCollection<string> Errors)
{
    public static UserAdministrationResult Success()
    {
        return new UserAdministrationResult(
            true,
            Array.Empty<string>());
    }

    public static UserAdministrationResult Failure(
        params string[] errors)
    {
        return new UserAdministrationResult(
            false,
            errors);
    }
}