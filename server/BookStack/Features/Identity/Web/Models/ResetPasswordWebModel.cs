namespace BookStack.Features.Identity.Web.Models;

using System.ComponentModel.DataAnnotations;

using static Identity.Shared.Constants;

/// <summary>
/// Request payload for completing password reset.
/// </summary>
public class ResetPasswordWebModel
{
    /// <summary>
    /// Email of the account whose password is being reset.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(
        Validation.EmailMaxLength,
        MinimumLength = Validation.EmailMinLength)]
    public string Email { get; init; } = default!;

    /// <summary>
    /// Base64-url encoded reset token received from the reset link.
    /// </summary>
    [Required]
    public string Token { get; init; } = default!;

    /// <summary>
    /// New raw password that will replace the old credential.
    /// </summary>
    [Required]
    [StringLength(
        Validation.PasswordMaxLength,
        MinimumLength = Validation.PasswordMinLength)]
    public string NewPassword { get; init; } = default!;
}
