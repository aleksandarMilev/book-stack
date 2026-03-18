namespace BookStack.Features.Identity.Data.Models;

using BookStack.Data.Models.Base;
using Microsoft.AspNetCore.Identity;
using SellerProfiles.Data.Models;
using UserProfile.Data.Models;

/// <summary>
/// Represents an application user stored in the database.
/// Inherits from <see cref="IdentityUser"/> and implements <see cref="IDeletableEntity"/>.
/// </summary>
/// <remarks>
/// Soft delete is implemented through <see cref="IsDeleted"/> and related audit fields, and enforced by a global query
/// filter configured in <c>UserDbModelConfiguration</c>.
/// </remarks>
public class UserDbModel:
    IdentityUser,
    IDeletableEntity
{
    /// <summary>
    /// The UTC date and time when the entity was created.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Identifier of the user who created the entity, if available.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// The UTC date and time when the entity was last modified, if any.
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Identifier of the user who last modified the entity, if available.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Indicates whether the entity has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The UTC date and time when the entity was deleted, if any.
    /// </summary>
    public DateTime? DeletedOn { get; set; }

    /// <summary>
    /// Identifier of the user who deleted the entity, if available.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Navigation property to the user's profile.
    /// </summary>
    public UserProfileDbModel? Profile { get; init; }

    /// <summary>
    /// Navigation property to the user's seller profile.
    /// </summary>
    public SellerProfileDbModel? SellerProfile { get; init; }
}
