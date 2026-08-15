namespace RTS.Application.Auditing;

public enum LoginFailureCode
{
    UserNotFound,
    AccountInactive,
    AccountDeleted,
    InvalidCredentials,
    AccountLockedOut,
    SignInNotAllowed
}