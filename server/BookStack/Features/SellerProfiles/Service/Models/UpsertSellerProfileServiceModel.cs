namespace BookStack.Features.SellerProfiles.Service.Models;

/// <summary>
/// Service-layer payload used to create or update a seller profile.
/// </summary>
public class UpsertSellerProfileServiceModel
{
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
    /// <remarks>
    /// Seller-profile business rules require at least one supported payment method; enforcement happens in service logic.
    /// </remarks>
    public bool SupportsCashOnDelivery { get; init; }
}
