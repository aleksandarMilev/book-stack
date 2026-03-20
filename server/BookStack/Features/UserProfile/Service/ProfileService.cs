namespace BookStack.Features.UserProfile.Service;

using BookStack.Data;
using Data.Models;
using Identity.Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.DateTimeProvider;
using Infrastructure.Services.ImageWriter;
using Infrastructure.Services.Result;
using Infrastructure.Services.StringSanitizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;
using Shared;

using static Common.Constants;
using static Shared.Constants;

/// <summary>
/// Implements user-profile read, update, and delete workflows.
/// </summary>
/// <remarks>
/// This service owns business behavior for "my profile" operations, image replace/remove behavior, and
/// coordinated soft-delete of both <see cref="UserProfileDbModel"/> and linked <see cref="UserDbModel"/> records.
/// </remarks>
public class ProfileService(
    BookStackDbContext data,
    UserManager<UserDbModel> userManager,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IImageWriter imageWriter,
    IStringSanitizerService stringSanitizer,
    ILogger<ProfileService> logger) : IProfileService
{
    /// <summary>
    /// Returns the profile for the current authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A profile service model for the current user, or <see langword="null"/> when no visible profile exists.
    /// </returns>
    public async Task<ProfileServiceModel?> Mine(
        CancellationToken cancellationToken = default)
        => await data
            .Profiles
            .AsNoTracking()
            .ToServiceModels()
            .SingleOrDefaultAsync(
                p => p.Id == currentUserService.GetId(),
                cancellationToken);

    /// <summary>
    /// Creates a profile for the supplied user id.
    /// </summary>
    /// <param name="serviceModel">Profile data to persist.</param>
    /// <param name="userId">Identifier of the user that will own the profile.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The created profile model.</returns>
    /// <remarks>
    /// This method is used by trusted internal flows, such as identity registration.
    /// </remarks>
    public async Task<ProfileServiceModel> Create(
        CreateProfileServiceModel serviceModel,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var dbModel = serviceModel.ToDbModel();
        dbModel.UserId = userId;

        await imageWriter.Write(
           resourceName: Paths.ProfilesImagePathPrefix,
           dbModel,
           serviceModel,
           Paths.DefaultImagePath,
           cancellationToken);

        data.Add(dbModel);
        await data.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Profile for user with id {UserId} was created.",
            userId);

        return dbModel.ToServiceModel();
    }

    /// <summary>
    /// Updates the current authenticated user's profile.
    /// </summary>
    /// <param name="serviceModel">Updated profile data with optional image remove/replace intent.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A success result when the profile was updated; otherwise a failure result with an error message.
    /// </returns>
    /// <remarks>
    /// If a new image is uploaded or image removal is requested, the previous non-default image is deleted when safe.
    /// </remarks>
    public async Task<Result> Edit(
        CreateProfileServiceModel serviceModel,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetId()!;
        var dbModel = await this.GetDbModel(
            userId,
            cancellationToken);

        if (dbModel is null)
        {
            return this.LogAndReturnNotFoundMessage(userId);
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
            await imageWriter.Write(
                resourceName: Paths.ProfilesImagePathPrefix,
                dbModel,
                serviceModel,
                defaultImagePath: null,
                cancellationToken);

            willDeleteOld &= serviceModel.Image is not null;
        }

        await data.SaveChangesAsync(cancellationToken);

        var shouldDeleteOldImage =
            willDeleteOld &&
            !string.Equals(
                oldImagePath,
                dbModel.ImagePath,
                StringComparison.OrdinalIgnoreCase);

        if (shouldDeleteOldImage)
        {
            var successfullyDeleted = imageWriter.Delete(
                Paths.ProfilesImagePathPrefix,
                oldImagePath,
                Paths.DefaultImagePath);

            if (!successfullyDeleted)
            {
                logger.LogWarning(
                    "Profile updated but old image was not deleted. UserId={UserId}, OldImagePath={OldImagePath}, NewImagePath={NewImagePath}",
                    userId,
                    oldImagePath,
                    dbModel.ImagePath);
            }
        }

        logger.LogInformation(
            "Profile updated. UserId={UserId}, RemoveImage={RemoveImage}, NewImageUploaded={NewImageUploaded}, ImagePath={ImagePath}",
            userId,
            serviceModel.RemoveImage,
            serviceModel.Image is not null,
            dbModel.ImagePath);

        return true;
    }

    /// <summary>
    /// Soft-deletes a profile and the linked identity user.
    /// </summary>
    /// <param name="userToDeleteId">
    /// Optional target user id. When not provided, the current authenticated user's profile is deleted.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A success result when delete operations complete; otherwise a failure result with an error message.
    /// </returns>
    /// <remarks>
    /// Non-admin users can delete only their own profile. Administrators can target other users.
    /// </remarks>
    public async Task<Result> Delete(
        string? userToDeleteId = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = currentUserService.GetId()!;
        userToDeleteId ??= currentUserId;

        var executionStrategy = data
            .Database
            .CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync<Result>(async () =>
        {
            await using var transaction = await data
                .Database
                .BeginTransactionAsync(cancellationToken);

            var profile = await this.GetDbModel(
                userToDeleteId,
                cancellationToken);

            if (profile is null)
            {
                return this.LogAndReturnNotFoundMessage(userToDeleteId);
            }

            var isNotCurrentUserProfile = profile.UserId != currentUserId;
            var userIsNotAdmin = !currentUserService.IsAdmin();

            if (isNotCurrentUserProfile && userIsNotAdmin)
            {
                return this.LogAndReturnUnauthorizedMessage(
                    currentUserId,
                    profile.UserId);
            }

            data.Remove(profile);
            await data.SaveChangesAsync(cancellationToken);

            var user = await userManager.FindByIdAsync(profile.UserId);
            if (user is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            user.IsDeleted = true;
            user.DeletedOn = dateTimeProvider.UtcNow;
            user.DeletedBy = currentUserService.GetUsername();
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var identityResult = await userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);

                var identityResultErrors = identityResult
                    .Errors
                    .Select(static e => e.Description);

                return string.Join("; ", identityResultErrors);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    private async Task<UserProfileDbModel?> GetDbModel(
        string userId,
        CancellationToken cancellationToken = default)
        => await data
            .Profiles
            .SingleOrDefaultAsync(
                p => p.UserId == userId,
                cancellationToken);

    private string LogAndReturnNotFoundMessage(string userId)
    {
        var sanitizedUserIdId = stringSanitizer
            .SanitizeStringForLog(userId);

        logger.LogWarning(
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
        var sanitizedCurrentUserId = stringSanitizer
            .SanitizeStringForLog(currentUserId);

        var sanitizedProfileId = stringSanitizer
            .SanitizeStringForLog(profileId);

        logger.LogWarning(
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
