using System.ComponentModel.DataAnnotations;

namespace RTS.Web.Configuration;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string CookieName { get; set; } = string.Empty;

    [Required]
    public string DataProtectionName { get; set; } = string.Empty;

    public string? DataProtectionKeysPath { get; set; }

    public string? DataProtectionCertificatePath
    {
        get;
        set;
    }

    public string? DataProtectionCertificatePassword
    {
        get;
        set;
    }
}