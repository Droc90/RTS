using System.ComponentModel.DataAnnotations;

namespace RTS.Web.Components.Account.Models;

public sealed class ResendConfirmationModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}