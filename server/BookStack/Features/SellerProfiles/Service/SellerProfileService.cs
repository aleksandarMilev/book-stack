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

public class SellerProfileService(
    BookStackDbContext data,
    ICurrentUserService userService,
    ILogger<SellerProfileService> logger) : ISellerProfileService
{
    public async Task<IEnumerable<SellerProfileServiceModel>> All(
        CancellationToken cancellationToken = default)
    {
        // the endpoint calling this method is admin protected. This path is not possbile to happen. Added as just good practice.
        if (!userService.IsAdmin())
        {
            return [];
        }

        return await data
            .SellerProfiles
            .AsNoTracking()
            .ToServiceModels()
            .OrderByDescending(static p => p.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<SellerProfileServiceModel?> ByUserId(
        string userId,
        CancellationToken cancellationToken = default)
    {
        // the endpoint calling this method is admin protected. This path is not possbile to happen. Added as just good practice.
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

    public async Task<ResultWith<SellerProfileServiceModel>> UpsertMine(
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default)
    {
        // the endpoint calling this method is auth protected. This path is not possbile to happen. Added just as good practice.
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

    //Used only internally. Does not accept the userId arg from the client. Safe to assume no malicous actions
    public async Task<ResultWith<SellerProfileServiceModel>> UpsertForUser(
        string userId,
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default)
        => await this.Upsert(userId, model, cancellationToken);

    public async Task<Result> ChangeStatus(
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        // the endpoint calling this method is admin protected. This path is not possbile to happen. Added just as good practice.
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

    public async Task<bool> HasActiveProfile(
        string userId,
        CancellationToken cancellationToken = default)
        => await data
            .SellerProfiles
            .AsNoTracking()
            .AnyAsync(
                p => p.UserId == userId && p.IsActive,
                cancellationToken);

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
