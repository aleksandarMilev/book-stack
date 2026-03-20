namespace BookStack.Features.SellerProfiles.Web.Models;

using System.ComponentModel.DataAnnotations;

using static Shared.Constants;

public class UpsertSellerProfileWebModel
{
    [Required]
    [StringLength(
        Validation.DisplayNameMaxLength,
        MinimumLength = Validation.DisplayNameMinLength)]
    public string DisplayName { get; init; } = default!;

    [StringLength(
        Validation.PhoneMaxLength,
        MinimumLength = Validation.PhoneMinLength)]
    public string? PhoneNumber { get; init; }

    public bool SupportsOnlinePayment { get; init; } = true;

    public bool SupportsCashOnDelivery { get; init; } = true;
}
