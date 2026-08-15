using System.ComponentModel.DataAnnotations;

namespace RTS.Web.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    [Range(1, 730)]
    public int HstsMaxAgeDays { get; set; } = 365;

    public bool HstsIncludeSubDomains { get; set; } = true;

    public bool HstsPreload { get; set; }
}