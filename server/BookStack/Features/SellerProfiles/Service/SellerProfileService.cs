namespace BookStack.Features.SellerProfiles.Service;

using BookStack.Data;
using Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.Result;
using Microsoft.EntityFrameworkCore;
using Models;
using Org.BouncyCastle.Security;
using Shared;

using static Common.Constants;

/// <summary>
/// Implements seller-profile read and management workflows.
/// </summary>
/// <remarks>
/// This service enforces seller capability rules such as payment-method support,
/// prerequisite user-profile existence, soft-delete boundaries, and admin-only status management.
/// </remarks>
public class SellerProfileService(
    BookStackDbContext data,
    ICurrentUserService userService,
    ILogger<SellerProfileService> logger) : ISellerProfileService
{
    /// <summary>
    /// Returns all visible seller profiles for administrator callers.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Seller profiles ordered by creation time descending for admins; otherwise an empty collection.
    /// </returns>
    /// <remarks>
    /// Controller authorization already protects this path; the non-admin branch is a defensive guard.
    /// </remarks>
    public async Task<IEnumerable<SellerProfileServiceModel>> All(
        CancellationToken cancellationToken = default)
    {
        if (!userService.IsAdmin())
        {
            return [];
        }

        return await data
            .SellerProfiles
            .AsNoTracking()
            .OrderByDescending(static p => p.CreatedOn)
            .ToServiceModels()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a seller profile by user id for administrator callers.
    /// </summary>
    /// <param name="userId">Identifier of the user that owns the seller profile.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Matching seller profile for admins; otherwise <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// Controller authorization already protects this path; the non-admin branch is a defensive guard.
    /// </remarks>
    public async Task<SellerProfileServiceModel?> ByUserId(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!userService.IsAdmin())
        {
            return null;
        }

        return await data
            .SellerProfiles
            .AsNoTracking()
            .ToServiceModels()
            .SingleOrDefaultAsync(
                p => p.UserId == userId,
                cancellationToken);
    }

    /// <summary>
    /// Returns the current authenticated user's seller profile.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Current user's seller profile when available and visible through query filters; otherwise <see langword="null"/>.
    /// </returns>
    public async Task<SellerProfileServiceModel?> Mine(
        CancellationToken cancellationToken = default)
    {
        var currentUserId = userService.GetId();
        if (currentUserId is null)
        {
            return null;
        }

        return await data
            .SellerProfiles
            .AsNoTracking()
            .ToServiceModels()
            .SingleOrDefaultAsync(
                p => p.UserId == currentUserId,
                cancellationToken);
    }

    /// <summary>
    /// Creates or updates the current authenticated user's seller profile.
    /// </summary>
    /// <param name="model">Editable seller-profile data.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Updated seller profile on success; otherwise a failure when authentication or business checks fail.
    /// </returns>
    /// <remarks>
    /// The endpoint is authenticated; the unauthenticated branch is a defensive guard.
    /// </remarks>
    public async Task<ResultWith<SellerProfileServiceModel>> UpsertMine(
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = userService.GetId();
        if (currentUserId is null)
        {
            return ErrorMessages.CurrentUserNotAuthenticated;
        }

        return await this.Upsert(
            currentUserId,
            model,
            cancellationToken);
    }

    /// <summary>
    /// Creates or updates a seller profile for a supplied user id in trusted internal flows.
    /// </summary>
    /// <param name="userId">Identifier of the user that will own the seller profile.</param>
    /// <param name="model">Editable seller-profile data.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Updated seller profile on success; otherwise a failure when business checks fail.
    /// </returns>
    /// <remarks>
    /// This method is not intended for user-supplied target ids from public client requests.
    /// </remarks>
    public async Task<ResultWith<SellerProfileServiceModel>> UpsertForUser(
        string userId,
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default)
        => await this.Upsert(userId, model, cancellationToken);

    /// <summary>
    /// Changes activation state for a seller profile.
    /// </summary>
    /// <param name="userId">Identifier of the user that owns the seller profile.</param>
    /// <param name="isActive"><see langword="true"/> to activate; <see langword="false"/> to deactivate.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Success when status is updated; otherwise a failure for unauthorized callers or missing seller profile.
    /// </returns>
    /// <remarks>
    /// Activation state is used by seller-capability checks (for example, listing prerequisites).
    /// </remarks>
    public async Task<Result> ChangeStatus(
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (!userService.IsAdmin())
        {
            return "Only administrators can change seller profile status.";
        }

        var profile = await data
            .SellerProfiles
            .SingleOrDefaultAsync(
                p => p.UserId == userId,
                cancellationToken);

        if (profile is null || profile.IsDeleted)
        {
            return string.Format(
                ErrorMessages.DbEntityNotFound,
                nameof(SellerProfileDbModel),
                userId);
        }

        profile.IsActive = isActive;
        await data.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seller profile status changed. UserId={UserId}, IsActive={IsActive}",
            userId,
            isActive);

        return true;
    }

    /// <summary>
    /// Checks whether a user currently has an active seller profile.
    /// </summary>
    /// <param name="userId">Identifier of the user to check.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> when an active seller profile exists; otherwise <see langword="false"/>.
    /// </returns>
    public async Task<bool> HasActiveProfile(
        string userId,
        CancellationToken cancellationToken = default)
        => await data
            .SellerProfiles
            .AsNoTracking()
            .AnyAsync(
                p => p.UserId == userId && p.IsActive,
                cancellationToken);

    /// <summary>
    /// Returns the active seller profile for a user.
    /// </summary>
    /// <param name="userId">Identifier of the user to query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Active seller profile when found; otherwise <see langword="null"/>.
    /// </returns>
    public async Task<SellerProfileServiceModel?> ActiveByUserId(
        string userId,
        CancellationToken cancellationToken = default)
        => await data
            .SellerProfiles
            .AsNoTracking()
            .ToServiceModels()
            .SingleOrDefaultAsync(
                p => p.UserId == userId && p.IsActive,
                cancellationToken);

    private async Task<ResultWith<SellerProfileServiceModel>> Upsert(
        string userId,
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default)
    {
        if (DoesNotSupportAnyPaymentOption(model))
        {
            return "Seller profile must support at least one payment method.";
        }

        var alreadyRegistered = await data
            .Profiles
            .AsNoTracking()
            .AnyAsync(
                u => u.UserId == userId,
                cancellationToken);

        if (!alreadyRegistered)
        {
            return "User can not create a SellerProfile without creating a UserProfile first.";
        }

        var profile = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                p => p.UserId == userId,
                cancellationToken);

        var wasSoftDeleted =
            profile is not null &&
            profile.IsDeleted == true;

        if (wasSoftDeleted)
        {
            return "User Profile was deleted. Can not beacome a seller before restoring";
        }

        if (profile is null)
        {
            profile = model.ToDbModel(userId);
            data.Add(profile);
        }
        else
        {
            model.UpdateDbModel(profile);
        }

        await data.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seller profile upserted. UserId={UserId}, IsActive={IsActive}, SupportsOnlinePayment={SupportsOnlinePayment}, SupportsCashOnDelivery={SupportsCashOnDelivery}",
            userId,
            profile.IsActive,
            profile.SupportsOnlinePayment,
            profile.SupportsCashOnDelivery);

        return ResultWith<SellerProfileServiceModel>
            .Success(profile.ToServiceModel());
    }

    private static bool DoesNotSupportAnyPaymentOption(
        UpsertSellerProfileServiceModel model)
        => !model.SupportsOnlinePayment && !model.SupportsCashOnDelivery;
}
