namespace BookStack.Features.UserProfile.Web.User.Models;

using System.ComponentModel.DataAnnotations;
using Infrastructure.Validation;

using static Shared.Constants;

/// <summary>
/// Request payload used to create or edit a user profile.
/// </summary>
public class CreateProfileWebModel
{
    /// <summary>
    /// User first name.
    /// </summary>
    [Required]
    [StringLength(
        Validation.NameMaxLength,
        MinimumLength = Validation.NameMinLength)]
    public string FirstName { get; init; } = default!;

    /// <summary>
    /// User last name.
    /// </summary>
    [Required]
    [StringLength(
        Validation.NameMaxLength,
        MinimumLength = Validation.NameMinLength)]
    public string LastName { get; init; } = default!;

    /// <summary>
    /// Optional profile image to upload.
    /// </summary>
    [ImageUpload]
    public IFormFile? Image { get; init; }

    /// <summary>
    /// Indicates whether the current profile image should be reset to the default image.
    /// </summary>
    public bool RemoveImage { get; init; }
}
