namespace RTS.Application.Identity;

public interface IAccountRequestLimiter
{
    bool TryAcquire(
        AccountRequestType requestType,
        string identifier);
}