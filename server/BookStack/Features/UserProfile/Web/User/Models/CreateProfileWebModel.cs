namespace BookStack.Features.UserProfile.Web.User.Models;

using System.ComponentModel.DataAnnotations;
using Infrastructure.Validation;

using static Shared.Constants;

public class CreateProfileWebModel
{
    [Required]
    [StringLength(
        Validation.NameMaxLength,
        MinimumLength = Validation.NameMinLength)]
    public string FirstName { get; init; } = default!;

    [Required]
    [StringLength(
        Validation.NameMaxLength,
        MinimumLength = Validation.NameMinLength)]
    public string LastName { get; init; } = default!;

    [ImageUpload]
    public IFormFile? Image { get; init; }

    public bool RemoveImage { get; init; }
}
