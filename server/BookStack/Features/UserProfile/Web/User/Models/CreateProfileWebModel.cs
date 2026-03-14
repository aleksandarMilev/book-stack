namespace BookStack.Features.UserProfile.Web.User.Models;

using System.ComponentModel.DataAnnotations;

using static Shared.Constants.Validation;

public class CreateProfileWebModel
{
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

    public IFormFile? Image { get; init; }

    public bool RemoveImage { get; init; }
}
