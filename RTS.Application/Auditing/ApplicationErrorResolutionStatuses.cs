namespace RTS.Application.Auditing;

public static class ApplicationErrorResolutionStatuses
{
    public const string New = "New";

    public const string Investigating = "Investigating";

    public const string Resolved = "Resolved";

    public const string Ignored = "Ignored";

    public static IReadOnlyCollection<string> All { get; } =
    [
        New,
        Investigating,
        Resolved,
        Ignored
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) &&
        All.Contains(
            status,
            StringComparer.Ordinal);
}