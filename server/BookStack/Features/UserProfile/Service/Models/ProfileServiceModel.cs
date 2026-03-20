namespace BookStack.Features.UserProfile.Service.Models;

/// <summary>
/// Read model returned by profile service operations.
/// </summary>
public class ProfileServiceModel
{
    /// <summary>
    /// Identifier of the user that owns the profile.
    /// </summary>
    public string Id { get; init; } = default!;

    /// <summary>
    /// Profile first name.
    /// </summary>
    public string FirstName { get; init; } = default!;

    /// <summary>
    /// Profile last name.
    /// </summary>
    public string LastName { get; init; } = default!;

    /// <summary>
    /// Relative path to the profile image.
    /// </summary>
    public string ImagePath { get; init; } = default!;
}
