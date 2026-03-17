namespace BookStack.Features.SellerProfiles.Data.Models;

using BookStack.Data.Models.Base;
using Identity.Data.Models;

public class SellerProfileDbModel : IDeletableEntity
{
    public string UserId { get; set; } = default!;

    public UserDbModel User { get; set; } = default!;

    public string DisplayName { get; set; } = default!;

    public string? PhoneNumber { get; set; }

    public bool SupportsOnlinePayment { get; set; }

    public bool SupportsCashOnDelivery { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedOn { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }
}
