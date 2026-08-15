namespace RTS.Contracts.Auditing;

public sealed record ApplicationErrorSearchResult(
    IReadOnlyCollection<ApplicationErrorSummary> Errors,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages =>
        TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                TotalCount / (double)PageSize);
}