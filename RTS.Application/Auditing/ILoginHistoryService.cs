namespace RTS.Application.Auditing;

public interface ILoginHistoryService
{
    Task RecordAsync(
        RecordLoginHistoryRequest request,
        CancellationToken cancellationToken = default);
}