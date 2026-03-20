namespace BookStack.Tests.SellerProfiles;

using BookStack.Features.SellerProfiles.Service.Models;
using BookStack.Features.SellerProfiles.Web.Models;

internal static class SellerProfilesTestData
{
    public static UpsertSellerProfileServiceModel CreateServiceModel(
        string displayName = "Alice Seller",
        string? phoneNumber = "+359888123456",
        bool supportsOnlinePayment = true,
        bool supportsCashOnDelivery = true)
        => new()
        {
            DisplayName = displayName,
            PhoneNumber = phoneNumber,
            SupportsOnlinePayment = supportsOnlinePayment,
            SupportsCashOnDelivery = supportsCashOnDelivery,
        };

    public static UpsertSellerProfileWebModel CreateWebModel(
        string displayName = "Alice Seller",
        string? phoneNumber = "+359888123456",
        bool supportsOnlinePayment = true,
        bool supportsCashOnDelivery = true)
        => new()
        {
            DisplayName = displayName,
            PhoneNumber = phoneNumber,
            SupportsOnlinePayment = supportsOnlinePayment,
            SupportsCashOnDelivery = supportsCashOnDelivery,
        };

    public static ChangeSellerProfileStatusWebModel CreateChangeStatusWebModel(
        bool isActive = true)
        => new()
        {
            IsActive = isActive,
        };
}
