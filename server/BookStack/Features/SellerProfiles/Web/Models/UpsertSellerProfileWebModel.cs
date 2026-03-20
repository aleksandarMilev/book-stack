namespace BookStack.Features.SellerProfiles.Web.Models;

using System.ComponentModel.DataAnnotations;

using static Shared.Constants;

/// <summary>
/// Request payload used to create or update the current authenticated user's seller profile.
/// </summary>
public class UpsertSellerProfileWebModel
{
    /// <summary>
    /// Seller-facing display name.
    /// </summary>
    [Required]
    [StringLength(
        Validation.DisplayNameMaxLength,
        MinimumLength = Validation.DisplayNameMinLength)]
    public string DisplayName { get; init; } = default!;

    /// <summary>
    /// Optional seller phone number.
    /// </summary>
    [StringLength(
        Validation.PhoneMaxLength,
        MinimumLength = Validation.PhoneMinLength)]
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Indicates whether the seller supports online payment.
    /// </summary>
    public bool SupportsOnlinePayment { get; init; } = true;

    /// <summary>
    /// Indicates whether the seller supports cash on delivery.
    /// </summary>
    /// <remarks>
    /// At least one payment method must be supported; this cross-field rule is enforced in service logic.
    /// </remarks>
    public bool SupportsCashOnDelivery { get; init; } = true;
}
