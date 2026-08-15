namespace RTS.Contracts.Auditing;

public sealed record UpdateApplicationErrorResult(
    bool Succeeded,
    ApplicationErrorDetails? Error,
    string? ErrorCode)
{
    public static UpdateApplicationErrorResult Success(
        ApplicationErrorDetails error) =>
        new(
            true,
            error,
            null);

    public static UpdateApplicationErrorResult Failure(
        string errorCode) =>
        new(
            false,
            null,
            errorCode);
}