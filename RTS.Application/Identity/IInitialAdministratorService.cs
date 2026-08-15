namespace RTS.Application.Identity;

public interface IInitialAdministratorService
{
    Task PromoteAsync(
        string email,
        CancellationToken cancellationToken = default);
}