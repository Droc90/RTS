namespace RTS.Contracts.Administration;

public sealed record UserSearchResult(
    IReadOnlyCollection<UserSummary> Users,
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