namespace BookStack.Features.Identity.Web.Models;

using System.ComponentModel.DataAnnotations;
using Infrastructure.Validation;

using static Shared.Constants.Validation;
using static UserProfile.Shared.Constants.Validation;

public class RegisterWebModel
{
    [Required]
    [StringLength(
        UsernameMaxLength,
        MinimumLength = UsernameMinLength)]
    public string Username { get; init; } = default!;

    [Required]
    [EmailAddress]
    [StringLength(
        EmailMaxLength,
        MinimumLength = EmailMinLength)]
    public string Email { get; init; } = default!;

    [Required]
    [StringLength(
        PasswordMaxLength,
        MinimumLength = PasswordMinLength)]
    public string Password { get; init; } = default!;

    [Required]
    [StringLength(
        NameMaxLength,
        MinimumLength = NameMinLength)]
    public string FirstName { get; init; } = default!;

    [Required]
    [StringLength(
        NameMaxLength,
        MinimumLength = NameMinLength)]
    public string LastName { get; init; } = default!;

    [ImageUpload]
    public IFormFile? Image { get; init; }
}
