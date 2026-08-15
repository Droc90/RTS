namespace RTS.Application.Auditing;

public interface IAuditService
{
    Task RecordAsync(
        RecordAuditRequest request,
        CancellationToken cancellationToken = default);
}