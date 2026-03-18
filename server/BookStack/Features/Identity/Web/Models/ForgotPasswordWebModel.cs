namespace BookStack.Features.Identity.Web.Models;

using System.ComponentModel.DataAnnotations;

using static Shared.Constants;

/// <summary>
/// Request payload for initiating password reset.
/// </summary>
public class ForgotPasswordWebModel
{
    /// <summary>
    /// Account email that should receive a password-reset link.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(
        Validation.EmailMaxLength,
        MinimumLength = Validation.EmailMinLength)]
    public string Email { get; init; } = default!;
}
