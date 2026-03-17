namespace BookStack.Features.SellerProfiles.Web.Models;

using System.ComponentModel.DataAnnotations;

using static Shared.Constants.Validation;

public class UpsertSellerProfileWebModel
{
    [Required]
    [StringLength(DisplayNameMaxLength, MinimumLength = DisplayNameMinLength)]
    public string DisplayName { get; init; } = default!;

    [StringLength(PhoneMaxLength, MinimumLength = PhoneMinLength)]
    public string? PhoneNumber { get; init; }

    public bool SupportsOnlinePayment { get; init; } = true;

    public bool SupportsCashOnDelivery { get; init; } = true;

    public bool IsActive { get; init; } = true;
}
