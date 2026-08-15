using System.ComponentModel.DataAnnotations;

namespace RTS.Web.Components.Account.Models;

public sealed class ResetPasswordModel
{
    [Required]
    [StringLength(
        100,
        MinimumLength = 10,
        ErrorMessage =
            "The password must be at least 10 characters long.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(
        nameof(NewPassword),
        ErrorMessage =
            "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}