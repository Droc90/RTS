namespace RTS.Infrastructure.Identity;

public sealed class AccountRequestLimitOptions
{
    public const string SectionName =
        "AccountRequestLimits";

    public int PermitLimit { get; set; } = 3;

    public int WindowMinutes { get; set; } = 15;
}