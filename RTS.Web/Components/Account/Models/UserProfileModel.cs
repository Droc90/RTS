using System.ComponentModel.DataAnnotations;

namespace RTS.Web.Components.Account.Models;

public sealed class UserProfileModel
{
    [Display(Name = "Display name")]
    [StringLength(150)]
    public string? DisplayName { get; set; }

    [Display(Name = "First name")]
    [StringLength(100)]
    public string? FirstName { get; set; }

    [Display(Name = "Last name")]
    [StringLength(100)]
    public string? LastName { get; set; }

    [Display(Name = "Time zone")]
    [StringLength(100)]
    public string? TimeZoneId { get; set; }

    [Display(Name = "Locale")]
    [StringLength(20)]
    public string? Locale { get; set; }

    [Display(Name = "Preferred date format")]
    [StringLength(50)]
    public string? PreferredDateFormat { get; set; }

    public byte[] RowVersion { get; set; } = [];
}