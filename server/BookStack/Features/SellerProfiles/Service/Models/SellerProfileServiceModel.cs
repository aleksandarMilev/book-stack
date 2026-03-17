namespace BookStack.Features.SellerProfiles.Service.Models;

public class SellerProfileServiceModel
{
    public string UserId { get; init; } = default!;

    public string DisplayName { get; init; } = default!;

    public string? PhoneNumber { get; init; }

    public bool SupportsOnlinePayment { get; init; }

    public bool SupportsCashOnDelivery { get; init; }

    public bool IsActive { get; init; }

    public string CreatedOn { get; init; } = default!;

    public string? ModifiedOn { get; init; }
}
