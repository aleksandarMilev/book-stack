namespace BookStack.Features.UserProfile.Service;

using BookStack.Data;
using Data.Models;
using Identity.Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.ImageWriter;
using Infrastructure.Services.Result;
using Infrastructure.Services.StringSanitizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;
using Shared;

using static Common.Constants;
using static Shared.Constants;

public class ProfileService(
    BookStackDbContext data,
    UserManager<UserDbModel> userManager,
    ICurrentUserService userService,
    IImageWriter imageWriter,
    IStringSanitizerService stringSanitizer,
    ILogger<ProfileService> logger) : IProfileService
{
    private readonly BookStackDbContext _data = data;
    private readonly UserManager<UserDbModel> _userManager = userManager;
    private readonly ICurrentUserService _userService = userService;
    private readonly IImageWriter _imageWriter = imageWriter;
    private readonly IStringSanitizerService _stringSanitizer = stringSanitizer;
    private readonly ILogger<ProfileService> _logger = logger;

    public async Task<ProfileServiceModel?> Mine(
        CancellationToken cancellationToken = default)
        => await this._data
            .Profiles
            .AsNoTracking()
            .ToServiceModels()
            .SingleOrDefaultAsync(
                p => p.Id == this._userService.GetId(),
                cancellationToken);

    public async Task<ProfileServiceModel?> OtherUser(
        string userId,
        CancellationToken cancellationToken = default)
        => await this._data
            .Profiles
            .AsNoTracking()
            .ToServiceModels()
            .SingleOrDefaultAsync(
                p => p.Id == userId,
                cancellationToken);

    public async Task<ProfileServiceModel> Create(
        CreateProfileServiceModel serviceModel,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var dbModel = serviceModel.ToDbModel();
        dbModel.UserId = userId;

        await this._imageWriter.Write(
           resourceName: Paths.ProfilesImagePathPrefix,
           dbModel,
           serviceModel,
           Paths.DefaultImagePath,
           cancellationToken);

        this._data.Add(dbModel);

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Profile for user with id {UserId} was created.",
            userId);

        return dbModel.ToServiceModel();
    }

    public async Task<Result> Edit(
        CreateProfileServiceModel serviceModel,
        CancellationToken cancellationToken = default)
    {
        var userId = this._userService.GetId()!;
        var dbModel = await this.GetDbModel(
            userId,
            cancellationToken);

        if (dbModel is null)
        {
            return this.LogAndReturnNotFoundMessage(userId);
        }

        if (dbModel.UserId != userId)
        {
            return this.LogAndReturnUnauthorizedMessage(
                userId,
                dbModel.UserId);
        }

        var oldImagePath = dbModel.ImagePath;

        serviceModel.UpdateDbModel(dbModel);

        var willDeleteOld =
            !string.IsNullOrWhiteSpace(oldImagePath) &&
            !string.Equals(
                oldImagePath,
                Paths.DefaultImagePath,
                StringComparison.OrdinalIgnoreCase);

        if (serviceModel.RemoveImage)
        {
            dbModel.ImagePath = Paths.DefaultImagePath;
        }
        else
        {
            await this._imageWriter.Write(
                resourceName: Paths.ProfilesImagePathPrefix,
                dbModel,
                serviceModel,
                defaultImagePath: null,
                cancellationToken);

            willDeleteOld &= serviceModel.Image is not null;
        }

        await this._data.SaveChangesAsync(cancellationToken);

        var shouldDeleteOldImage = 
            willDeleteOld &&
            !string.Equals(
                oldImagePath,
                dbModel.ImagePath,
                StringComparison.OrdinalIgnoreCase);

        if (shouldDeleteOldImage)
        {
            var successfullyDeleted = this._imageWriter.Delete(
                Paths.ProfilesImagePathPrefix,
                oldImagePath,
                Paths.DefaultImagePath);

            if (!successfullyDeleted)
            {
                this._logger.LogWarning(
                    "Profile updated but old image was not deleted. UserId={UserId}, OldImagePath={OldImagePath}, NewImagePath={NewImagePath}",
                    userId,
                    oldImagePath,
                    dbModel.ImagePath);
            }
        }

        this._logger.LogInformation(
            "Profile updated. UserId={UserId}, RemoveImage={RemoveImage}, NewImageUploaded={NewImageUploaded}, ImagePath={ImagePath}",
            userId,
            serviceModel.RemoveImage,
            serviceModel.Image is not null,
            dbModel.ImagePath);

        return true;
    }

    public async Task<Result> Delete(
        string? userToDeleteId = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId()!;
        userToDeleteId ??= currentUserId;

        var profile = await this.GetDbModel(
            userToDeleteId,
            cancellationToken);

        if (profile is null)
        {
            return this.LogAndReturnNotFoundMessage(userToDeleteId);
        }

        var isNotCurrentUserProfile = profile.UserId != currentUserId;
        var userIsNotAdmin = !this._userService.IsAdmin();

        if (isNotCurrentUserProfile && userIsNotAdmin)
        {
            return this.LogAndReturnUnauthorizedMessage(
                currentUserId,
                profile.UserId);
        }

        this._data.Remove(profile);

        var user = await this._userManager
            .FindByIdAsync(profile.UserId);

        if (user is null)
        {
            return false;
        }

        var identityResult = await this._userManager
            .DeleteAsync(user);

        if (!identityResult.Succeeded)
        {
            var identityResultErrors = identityResult
                .Errors
                .Select(static e => e.Description);

            return string.Join("; ", identityResultErrors);
        }

        return true;
    }

    private async Task<UserProfileDbModel?> GetDbModel(
        string id,
        CancellationToken cancellationToken = default)
        => await this._data
            .Profiles
            .FindAsync([id], cancellationToken);

    private string LogAndReturnNotFoundMessage(string userId)
    {
        var sanitizedUserIdId = this._stringSanitizer
            .SanitizeStringForLog(userId);

        this._logger.LogWarning(
            ErrorMessages.DbEntityNotFoundTemplate,
            nameof(UserProfileDbModel),
            sanitizedUserIdId);

        return string.Format(
            ErrorMessages.DbEntityNotFound,
            nameof(UserProfileDbModel),
            sanitizedUserIdId);
    }

    private string LogAndReturnUnauthorizedMessage(
        string currentUserId,
        string profileId)
    {
        var sanitizedCurrentUserId = this._stringSanitizer
            .SanitizeStringForLog(currentUserId);

        var sanitizedProfileId = this._stringSanitizer
            .SanitizeStringForLog(profileId);

        this._logger.LogWarning(
            ErrorMessages.UnauthorizedMessageTemplate,
            sanitizedCurrentUserId,
            nameof(UserProfileDbModel),
            sanitizedProfileId);

        return string.Format(
            ErrorMessages.UnauthorizedMessage,
            sanitizedCurrentUserId,
            nameof(UserProfileDbModel),
            sanitizedProfileId);
    }
}
