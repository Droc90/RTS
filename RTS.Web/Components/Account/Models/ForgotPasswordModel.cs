using System.ComponentModel.DataAnnotations;

namespace RTS.Web.Components.Account.Models;

public sealed class ForgotPasswordModel
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}