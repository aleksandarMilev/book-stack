namespace BookStack.Features.SellerProfiles.Shared;

using Data.Models;
using Service.Models;
using Web.Models;

public static class SellerProfileMapping
{
    public static IQueryable<SellerProfileServiceModel> ToServiceModels(
        this IQueryable<SellerProfileDbModel> dbModels)
        => dbModels.Select(static p => new SellerProfileServiceModel
        {
            UserId = p.UserId,
            DisplayName = p.DisplayName,
            PhoneNumber = p.PhoneNumber,
            SupportsOnlinePayment = p.SupportsOnlinePayment,
            SupportsCashOnDelivery = p.SupportsCashOnDelivery,
            IsActive = p.IsActive,
            CreatedOn = p.CreatedOn.ToString("O"),
            ModifiedOn = p.ModifiedOn.HasValue
                ? p.ModifiedOn.Value.ToString("O")
                : null,
        });

    public static SellerProfileServiceModel ToServiceModel(
        this SellerProfileDbModel dbModel)
        => new()
        {
            UserId = dbModel.UserId,
            DisplayName = dbModel.DisplayName,
            PhoneNumber = dbModel.PhoneNumber,
            SupportsOnlinePayment = dbModel.SupportsOnlinePayment,
            SupportsCashOnDelivery = dbModel.SupportsCashOnDelivery,
            IsActive = dbModel.IsActive,
            CreatedOn = dbModel.CreatedOn.ToString("O"),
            ModifiedOn = dbModel.ModifiedOn.HasValue
                ? dbModel.ModifiedOn.Value.ToString("O")
                : null,
        };

    public static SellerProfileDbModel ToDbModel(
        this UpsertSellerProfileServiceModel serviceModel,
        string userId)
        => new()
        {
            UserId = userId,
            DisplayName = serviceModel.DisplayName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(serviceModel.PhoneNumber)
                ? null
                : serviceModel.PhoneNumber.Trim(),
            SupportsOnlinePayment = serviceModel.SupportsOnlinePayment,
            SupportsCashOnDelivery = serviceModel.SupportsCashOnDelivery,
            IsActive = serviceModel.IsActive,
        };

    public static void UpdateDbModel(
        this UpsertSellerProfileServiceModel serviceModel,
        SellerProfileDbModel dbModel)
    {
        dbModel.DisplayName = serviceModel.DisplayName.Trim();
        dbModel.PhoneNumber = string.IsNullOrWhiteSpace(serviceModel.PhoneNumber)
            ? null
            : serviceModel.PhoneNumber.Trim();
        dbModel.SupportsOnlinePayment = serviceModel.SupportsOnlinePayment;
        dbModel.SupportsCashOnDelivery = serviceModel.SupportsCashOnDelivery;
        dbModel.IsActive = serviceModel.IsActive;
    }

    public static UpsertSellerProfileServiceModel ToUpsertServiceModel(
        this UpsertSellerProfileWebModel webModel)
        => new()
        {
            DisplayName = webModel.DisplayName,
            PhoneNumber = webModel.PhoneNumber,
            SupportsOnlinePayment = webModel.SupportsOnlinePayment,
            SupportsCashOnDelivery = webModel.SupportsCashOnDelivery,
            IsActive = webModel.IsActive,
        };
}
