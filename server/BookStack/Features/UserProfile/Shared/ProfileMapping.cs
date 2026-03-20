namespace BookStack.Features.UserProfile.Shared;

using Data.Models;
using Service.Models;
using Web.User.Models;

/// <summary>
/// Mapping helpers between user-profile web, service, and database models.
/// </summary>
public static class ProfileMapping
{
    /// <summary>
    /// Projects profile database models to profile service models.
    /// </summary>
    /// <param name="dbModels">Profile query to project.</param>
    /// <returns>Projected query of <see cref="ProfileServiceModel"/> values.</returns>
    public static IQueryable<ProfileServiceModel> ToServiceModels(
        this IQueryable<UserProfileDbModel> dbModels)
        => dbModels.Select(static p => new ProfileServiceModel
        {
            Id = p.UserId,
            FirstName = p.FirstName,
            LastName = p.LastName,
            ImagePath = p.ImagePath,
        });

    /// <summary>
    /// Maps a profile database model to a profile service model.
    /// </summary>
    /// <param name="dbModel">Source profile entity.</param>
    /// <returns>Mapped <see cref="ProfileServiceModel"/>.</returns>
    public static ProfileServiceModel ToServiceModel(
        this UserProfileDbModel dbModel)
        => new()
        {
            Id = dbModel.UserId,
            FirstName = dbModel.FirstName,
            LastName = dbModel.LastName,
            ImagePath = dbModel.ImagePath,
        };

    /// <summary>
    /// Maps profile service input to a new profile database model.
    /// </summary>
    /// <param name="serviceModel">Source service model.</param>
    /// <returns>New <see cref="UserProfileDbModel"/> instance.</returns>
    public static UserProfileDbModel ToDbModel(
        this CreateProfileServiceModel serviceModel)
        => new()
        {
            FirstName = serviceModel.FirstName,
            LastName = serviceModel.LastName,
        };

    /// <summary>
    /// Applies editable service-model fields to an existing profile entity.
    /// </summary>
    /// <param name="serviceModel">Source service model with updated name values.</param>
    /// <param name="dbModel">Target profile entity to update.</param>
    public static void UpdateDbModel(
        this CreateProfileServiceModel serviceModel,
        UserProfileDbModel dbModel)
    {
        dbModel.FirstName = serviceModel.FirstName;
        dbModel.LastName = serviceModel.LastName;
    }

    /// <summary>
    /// Maps profile web input to profile service input.
    /// </summary>
    /// <param name="webModel">Incoming web model from the profile endpoint.</param>
    /// <returns>Mapped <see cref="CreateProfileServiceModel"/>.</returns>
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
