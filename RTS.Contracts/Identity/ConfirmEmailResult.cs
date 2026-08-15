namespace RTS.Contracts.Identity;

public sealed record ConfirmEmailResult(
    bool Succeeded,
    IReadOnlyCollection<string> Errors);