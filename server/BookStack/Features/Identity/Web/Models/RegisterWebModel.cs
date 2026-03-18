namespace BookStack.Features.Identity.Web.Models;

using System.ComponentModel.DataAnnotations;
using Infrastructure.Validation;

using static Shared.Constants.Validation;
using static UserProfile.Shared.Constants.Validation;

/// <summary>
/// Request payload for account registration.
/// </summary>
public class RegisterWebModel
{
    /// <summary>
    /// Desired public username for the new account.
    /// </summary>
    [Required]
    [StringLength(
        UsernameMaxLength,
        MinimumLength = UsernameMinLength)]
    public string Username { get; init; } = default!;

    /// <summary>
    /// Email address used for account communication and credential recovery.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(
        EmailMaxLength,
        MinimumLength = EmailMinLength)]
    public string Email { get; init; } = default!;

    /// <summary>
    /// Raw password used for initial account creation.
    /// </summary>
    [Required]
    [StringLength(
        PasswordMaxLength,
        MinimumLength = PasswordMinLength)]
    public string Password { get; init; } = default!;

    /// <summary>
    /// User first name stored in the linked profile.
    /// </summary>
    [Required]
    [StringLength(
        NameMaxLength,
        MinimumLength = NameMinLength)]
    public string FirstName { get; init; } = default!;

    /// <summary>
    /// User last name stored in the linked profile.
    /// </summary>
    [Required]
    [StringLength(
        NameMaxLength,
        MinimumLength = NameMinLength)]
    public string LastName { get; init; } = default!;

    /// <summary>
    /// Optional profile image uploaded during registration.
    /// </summary>
    [ImageUpload]
    public IFormFile? Image { get; init; }
}
