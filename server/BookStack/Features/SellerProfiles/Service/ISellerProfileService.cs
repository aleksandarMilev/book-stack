namespace BookStack.Features.SellerProfiles.Service;

using Infrastructure.Services.Result;
using Infrastructure.Services.ServiceLifetimes;
using Models;

public interface ISellerProfileService : IScopedService
{
    Task<IEnumerable<SellerProfileServiceModel>> All(
        CancellationToken cancellationToken = default);

    Task<SellerProfileServiceModel?> ByUserId(
        string userId,
        CancellationToken cancellationToken = default);

    Task<SellerProfileServiceModel?> Mine(
        CancellationToken cancellationToken = default);

    Task<ResultWith<SellerProfileServiceModel>> UpsertMine(
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default);

    Task<ResultWith<SellerProfileServiceModel>> UpsertForUser(
        string userId,
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default);

    Task<Result> ChangeStatus(
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveProfile(
        string userId,
        CancellationToken cancellationToken = default);

    Task<SellerProfileServiceModel?> ActiveByUserId(
        string userId,
        CancellationToken cancellationToken = default);
}
