namespace RTS.Contracts.Identity;

public sealed record ResetPasswordResult(
    bool Succeeded,
    IReadOnlyCollection<string> Errors);