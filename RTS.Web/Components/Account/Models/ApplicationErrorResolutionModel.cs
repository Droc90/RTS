using System.ComponentModel.DataAnnotations;

namespace RTS.Web.Components.Account.Models;

public sealed class ApplicationErrorResolutionModel
{
    [Required]
    [Display(Name = "Resolution status")]
    public string ResolutionStatus { get; set; } =
        string.Empty;

    [StringLength(2000)]
    [Display(Name = "Resolution notes")]
    public string? ResolutionNotes { get; set; }

    public byte[] RowVersion { get; set; } = [];
}