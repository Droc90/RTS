namespace RTS.Application.Auditing;

public interface IApplicationErrorService
{
    Task RecordAsync(
        RecordApplicationErrorRequest request,
        CancellationToken cancellationToken = default);
}