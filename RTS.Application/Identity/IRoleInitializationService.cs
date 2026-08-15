namespace RTS.Application.Identity;

public interface IRoleInitializationService
{
    Task InitializeAsync(
        CancellationToken cancellationToken = default);
}