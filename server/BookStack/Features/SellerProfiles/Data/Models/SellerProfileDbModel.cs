namespace BookStack.Features.SellerProfiles.Data.Models;

using BookStack.Data.Models.Base;
using Identity.Data.Models;

/// <summary>
/// Represents the seller business profile linked one-to-one to a user account.
/// </summary>
/// <remarks>
/// A seller profile is a distinct capability from basic authentication and stores seller-facing data such as
/// public display name, supported buyer payment methods, activation state, and soft-delete metadata.
/// </remarks>
public class SellerProfileDbModel : IDeletableEntity
{
    /// <summary>
    /// Foreign key to the owning identity user.
    /// </summary>
    public string UserId { get; set; } = default!;

    /// <summary>
    /// Navigation property to the owning identity user.
    /// </summary>
    public UserDbModel User { get; set; } = default!;

    /// <summary>
    /// Seller-facing display name shown in marketplace contexts.
    /// </summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>
    /// Optional seller phone number used for operational communication.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Indicates whether the seller accepts online payment.
    /// </summary>
    public bool SupportsOnlinePayment { get; set; }

    /// <summary>
    /// Indicates whether the seller accepts cash on delivery.
    /// </summary>
    public bool SupportsCashOnDelivery { get; set; }

    /// <summary>
    /// Indicates whether the seller capability is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Indicates whether the seller profile has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when the seller profile was soft-deleted.
    /// </summary>
    public DateTime? DeletedOn { get; set; }

    /// <summary>
    /// Username of the actor that soft-deleted the seller profile.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// UTC timestamp when the seller profile was created.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Username of the actor that created the seller profile.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// UTC timestamp when the seller profile was last modified.
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Username of the actor that last modified the seller profile.
    /// </summary>
    public string? ModifiedBy { get; set; }
}
