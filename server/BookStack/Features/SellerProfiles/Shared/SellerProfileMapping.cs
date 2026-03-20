namespace BookStack.Features.SellerProfiles.Shared;

using Data.Models;
using Service.Models;
using Web.Models;

/// <summary>
/// Mapping helpers between seller-profile web, service, and database models.
/// </summary>
public static class SellerProfileMapping
{
    /// <summary>
    /// Projects seller-profile entities to seller-profile service models.
    /// </summary>
    /// <param name="dbModels">Seller-profile query to project.</param>
    /// <returns>Projected query of <see cref="SellerProfileServiceModel"/> values.</returns>
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

    /// <summary>
    /// Maps a seller-profile entity to a seller-profile service model.
    /// </summary>
    /// <param name="dbModel">Source seller-profile entity.</param>
    /// <returns>Mapped <see cref="SellerProfileServiceModel"/>.</returns>
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

    /// <summary>
    /// Maps seller-profile service input to a new seller-profile entity.
    /// </summary>
    /// <param name="serviceModel">Source service model.</param>
    /// <param name="userId">Identifier of the user that will own the seller profile.</param>
    /// <returns>New <see cref="SellerProfileDbModel"/> instance.</returns>
    /// <remarks>
    /// Display name and phone number are trimmed; blank phone numbers are normalized to <see langword="null"/>.
    /// New profiles start as active.
    /// </remarks>
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
            IsActive = true,
        };

    /// <summary>
    /// Applies editable service-model fields to an existing seller-profile entity.
    /// </summary>
    /// <param name="serviceModel">Source service model with updated values.</param>
    /// <param name="dbModel">Target seller-profile entity to update.</param>
    /// <remarks>
    /// Display name and phone number are trimmed; blank phone numbers are normalized to <see langword="null"/>.
    /// </remarks>
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
    }

    /// <summary>
    /// Maps seller-profile web input to seller-profile service input.
    /// </summary>
    /// <param name="webModel">Incoming web model from seller-profile endpoints.</param>
    /// <returns>Mapped <see cref="UpsertSellerProfileServiceModel"/>.</returns>
    public static UpsertSellerProfileServiceModel ToUpsertServiceModel(
        this UpsertSellerProfileWebModel webModel)
        => new()
        {
            DisplayName = webModel.DisplayName,
            PhoneNumber = webModel.PhoneNumber,
            SupportsOnlinePayment = webModel.SupportsOnlinePayment,
            SupportsCashOnDelivery = webModel.SupportsCashOnDelivery,
        };
}
