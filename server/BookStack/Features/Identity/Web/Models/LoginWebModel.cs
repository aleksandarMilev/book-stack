namespace BookStack.Features.Identity.Web.Models;

using System.ComponentModel.DataAnnotations;

using static Shared.Constants;

/// <summary>
/// Request payload for user authentication.
/// </summary>
public class LoginWebModel
{
    /// <summary>
    /// Username or email used to identify the account.
    /// </summary>
    [Required]
    [StringLength(
       Validation.CredentialsMaxLength,
       MinimumLength = Validation.CredentialsMinLength)]
    public string Credentials { get; init; } = default!;

    /// <summary>
    /// Raw password supplied for authentication.
    /// </summary>
    [Required]
    [StringLength(
       Validation.PasswordMaxLength,
       MinimumLength = Validation.PasswordMinLength)]
    public string Password { get; init; } = default!;

    /// <summary>
    /// Indicates whether the issued token should use extended expiration.
    /// </summary>
    public bool RememberMe { get; init; }
}
