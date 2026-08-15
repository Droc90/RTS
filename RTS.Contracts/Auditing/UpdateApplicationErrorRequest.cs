namespace RTS.Contracts.Auditing;

public sealed record UpdateApplicationErrorRequest(
    string ResolutionStatus,
    string? ResolutionNotes,
    byte[] RowVersion);