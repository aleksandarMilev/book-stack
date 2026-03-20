namespace BookStack.Features.UserProfile.Service.Models;

using Infrastructure.Services.ImageWriter.Models;

/// <summary>
/// Service-layer payload for creating or editing a user profile.
/// </summary>
public class CreateProfileServiceModel : IImageServiceModel
{
    /// <summary>
    /// Profile first name.
    /// </summary>
    public string FirstName { get; init; } = default!;

    /// <summary>
    /// Profile last name.
    /// </summary>
    public string LastName { get; init; } = default!;

    /// <summary>
    /// Optional uploaded profile image.
    /// </summary>
    public IFormFile? Image { get; init; }

    /// <summary>
    /// Indicates whether the current profile image should be replaced with the default image.
    /// </summary>
    public bool RemoveImage { get; init; }
}
