namespace BookStack.Features.UserProfile.Service;

using Infrastructure.Services.Result;
using Infrastructure.Services.ServiceLifetimes;
using Models;

public interface IProfileService : IScopedService
{
    Task<ProfileServiceModel?> Mine(
        CancellationToken cancellationToken = default);

    Task<ProfileServiceModel?> OtherUser(
        string id,
        CancellationToken cancellationToken = default);

    Task<ProfileServiceModel> Create(
        CreateProfileServiceModel model,
        string userId,
        CancellationToken cancellationToken = default);

    Task<Result> Edit(
        CreateProfileServiceModel model,
        CancellationToken cancellationToken = default);

    Task<Result> Delete(
        string? userId = null,
        CancellationToken cancellationToken = default);
}
