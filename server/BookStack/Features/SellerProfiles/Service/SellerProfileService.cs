namespace BookStack.Features.SellerProfiles.Service;

using BookStack.Data;
using Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.Result;
using Microsoft.EntityFrameworkCore;
using Shared;
using Models;

using static Common.Constants;

public class SellerProfileService(
    BookStackDbContext data,
    ICurrentUserService userService,
    ILogger<SellerProfileService> logger) : ISellerProfileService
{
    private readonly BookStackDbContext _data = data;
    private readonly ICurrentUserService _userService = userService;
    private readonly ILogger<SellerProfileService> _logger = logger;

    public async Task<IEnumerable<SellerProfileServiceModel>> All(
        CancellationToken cancellationToken = default)
        => await this
            ._data
            .SellerProfiles
            .AsNoTracking()
            .OrderByDescending(static p => p.CreatedOn)
            .ToServiceModels()
            .ToListAsync(cancellationToken);

    public async Task<SellerProfileServiceModel?> ByUserId(
        string userId,
        CancellationToken cancellationToken = default)
        => await this
            ._data
            .SellerProfiles
            .AsNoTracking()
            .ToServiceModels()
            .SingleOrDefaultAsync(
                p => p.UserId == userId,
                cancellationToken);

    public async Task<SellerProfileServiceModel?> Mine(
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return null;
        }

        return await this.ByUserId(
            currentUserId,
            cancellationToken);
    }

    public async Task<ResultWith<SellerProfileServiceModel>> UpsertMine(
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return ErrorMessages.CurrentUserNotAuthenticated;
        }

        if (!model.SupportsOnlinePayment && !model.SupportsCashOnDelivery)
        {
            return "Seller profile must support at least one payment method.";
        }

        var profile = await this._data
            .SellerProfiles
            .SingleOrDefaultAsync(
                p => p.UserId == currentUserId,
                cancellationToken);

        if (profile is null)
        {
            profile = model.ToDbModel(currentUserId);
            this._data.Add(profile);
        }
        else
        {
            model.UpdateDbModel(profile);
        }

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Seller profile upserted. UserId={UserId}, IsActive={IsActive}, SupportsOnlinePayment={SupportsOnlinePayment}, SupportsCashOnDelivery={SupportsCashOnDelivery}",
            currentUserId,
            profile.IsActive,
            profile.SupportsOnlinePayment,
            profile.SupportsCashOnDelivery);

        return ResultWith<SellerProfileServiceModel>
            .Success(profile.ToServiceModel());
    }

    public async Task<Result> ChangeStatus(
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var profile = await this._data
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
        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Seller profile status changed. UserId={UserId}, IsActive={IsActive}",
            userId,
            isActive);

        return true;
    }

    public async Task<bool> HasActiveProfile(
        string userId,
        CancellationToken cancellationToken = default)
        => await this._data
            .SellerProfiles
            .AsNoTracking()
            .AnyAsync(
                p => p.UserId == userId && p.IsActive,
                cancellationToken);

    public async Task<SellerProfileServiceModel?> ActiveByUserId(
        string userId,
        CancellationToken cancellationToken = default)
        => await this._data
            .SellerProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsActive)
            .ToServiceModels()
            .SingleOrDefaultAsync(cancellationToken);
}
