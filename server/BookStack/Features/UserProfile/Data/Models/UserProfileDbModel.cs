namespace BookStack.Features.UserProfile.Data.Models;

using BookStack.Data.Models.Base;
using Identity.Data.Models;
using Infrastructure.Services.ImageWriter.Models;

/// <summary>
/// Represents the general personal profile for an authenticated user.
/// </summary>
/// <remarks>
/// This entity is linked one-to-one with <see cref="UserDbModel"/> through <see cref="UserId"/>.
/// Soft delete is implemented via <see cref="IsDeleted"/> and the related audit fields.
/// </remarks>
public class UserProfileDbModel:
    IDeletableEntity,
    IImageDbModel
{
    /// <summary>
    /// Indicates whether the profile has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when the profile was soft-deleted.
    /// </summary>
    public DateTime? DeletedOn { get; set; }

    /// <summary>
    /// Username of the actor that soft-deleted the profile.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Foreign key to the linked identity user.
    /// </summary>
    public string UserId { get; set; } = default!;

    /// <summary>
    /// Navigation property to the linked identity user.
    /// </summary>
    public UserDbModel User { get; set; } = default!;

    /// <summary>
    /// User first name.
    /// </summary>
    public string FirstName { get; set; } = default!;

    /// <summary>
    /// User last name.
    /// </summary>
    public string LastName { get; set; } = default!;

    /// <summary>
    /// Relative path to the profile image used for presentation.
    /// </summary>
    public string ImagePath { get; set; } = default!;

    /// <summary>
    /// UTC timestamp when the profile was created.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Username of the actor that created the profile.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// UTC timestamp when the profile was last modified.
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Username of the actor that last modified the profile.
    /// </summary>
    public string? ModifiedBy { get; set; }
}
