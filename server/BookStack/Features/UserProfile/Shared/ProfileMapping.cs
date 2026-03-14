namespace BookStack.Features.UserProfile.Shared;

using Data.Models;
using Service.Models;
using Web.User.Models;

public static class ProfileMapping
{
    public static IQueryable<ProfileServiceModel> ToServiceModels(
        this IQueryable<UserProfileDbModel> dbModels)
        => dbModels.Select(static p => new ProfileServiceModel
        {
            Id = p.UserId,
            FirstName = p.FirstName,
            LastName = p.LastName,
            ImagePath = p.ImagePath,
        });

    public static ProfileServiceModel ToServiceModel(
        this UserProfileDbModel dbModel)
        => new()
        {
            Id = dbModel.UserId,
            FirstName = dbModel.FirstName,
            LastName = dbModel.LastName,
            ImagePath = dbModel.ImagePath,
        };


    public static UserProfileDbModel ToDbModel(
        this CreateProfileServiceModel serviceModel)
        => new()
        {
            FirstName = serviceModel.FirstName,
            LastName = serviceModel.LastName,
        };

    public static void UpdateDbModel(
        this CreateProfileServiceModel serviceModel,
        UserProfileDbModel dbModel)
    {
        dbModel.FirstName = serviceModel.FirstName;
        dbModel.LastName = serviceModel.LastName;
    }

    public static CreateProfileServiceModel ToCreateServiceModel(
        this CreateProfileWebModel webModel)
        => new()
        {
            FirstName = webModel.FirstName,
            LastName = webModel.LastName,
            RemoveImage = webModel.RemoveImage,
            Image = webModel.Image
        };
}
