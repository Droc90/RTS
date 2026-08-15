namespace RTS.Application.Auditing;

public enum LoginEventType
{
    LoginSucceeded,
    LoginFailed,
    LoginLockedOut,
    LogoutSucceeded
}