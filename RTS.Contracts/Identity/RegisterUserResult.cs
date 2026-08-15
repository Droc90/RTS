namespace RTS.Contracts.Identity;

public sealed record RegisterUserResult(
    bool Succeeded,
    Guid? UserExternalId,
    IReadOnlyCollection<string> Errors);
