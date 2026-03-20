namespace BookStack.Features.SellerProfiles.Service.Models;

/// <summary>
/// Read model returned by seller-profile service operations.
/// </summary>
public class SellerProfileServiceModel
{
    /// <summary>
    /// Identifier of the user that owns the seller profile.
    /// </summary>
    public string UserId { get; init; } = default!;

    /// <summary>
    /// Seller-facing display name.
    /// </summary>
    public string DisplayName { get; init; } = default!;

    /// <summary>
    /// Optional seller phone number.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Indicates whether the seller supports online payment.
    /// </summary>
    public bool SupportsOnlinePayment { get; init; }

    /// <summary>
    /// Indicates whether the seller supports cash on delivery.
    /// </summary>
    public bool SupportsCashOnDelivery { get; init; }

    /// <summary>
    /// Indicates whether the seller profile is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// UTC creation timestamp in round-trip (<c>O</c>) string format.
    /// </summary>
    public string CreatedOn { get; init; } = default!;

    /// <summary>
    /// UTC modification timestamp in round-trip (<c>O</c>) string format, when available.
    /// </summary>
    public string? ModifiedOn { get; init; }
}
