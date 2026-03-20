namespace BookStack.Features.SellerProfiles.Service;

using Infrastructure.Services.Result;
using Infrastructure.Services.ServiceLifetimes;
using Models;

/// <summary>
/// Provides operations for managing seller capability profiles.
/// </summary>
/// <remarks>
/// Seller profile behavior is distinct from base authentication and includes payment-method support,
/// activation state, and admin-scoped management paths.
/// </remarks>
public interface ISellerProfileService : IScopedService
{
    /// <summary>
    /// Returns all visible seller profiles in descending creation order.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Seller profiles for administrator callers; an empty collection for non-admin callers in the defensive service path.
    /// </returns>
    Task<IEnumerable<SellerProfileServiceModel>> All(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a seller profile by target user id.
    /// </summary>
    /// <param name="userId">Identifier of the user that owns the seller profile.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Matching seller profile for administrator callers; otherwise <see langword="null"/>.
    /// </returns>
    Task<SellerProfileServiceModel?> ByUserId(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the seller profile of the current authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Current user's seller profile when visible through query filters; otherwise <see langword="null"/>.
    /// </returns>
    Task<SellerProfileServiceModel?> Mine(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates the current authenticated user's seller profile.
    /// </summary>
    /// <param name="model">Editable seller-profile data.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Updated seller profile when successful; otherwise a failure describing rejected business rules.
    /// </returns>
    Task<ResultWith<SellerProfileServiceModel>> UpsertMine(
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a seller profile for a specific user id in trusted internal flows.
    /// </summary>
    /// <param name="userId">Identifier of the user that will own the seller profile.</param>
    /// <param name="model">Editable seller-profile data.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Updated seller profile when successful; otherwise a failure describing rejected business rules.
    /// </returns>
    /// <remarks>
    /// This method is intended for internal orchestration and does not assume user id input from a public client.
    /// </remarks>
    Task<ResultWith<SellerProfileServiceModel>> UpsertForUser(
        string userId,
        UpsertSellerProfileServiceModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes seller-profile activation state for a target user.
    /// </summary>
    /// <param name="userId">Identifier of the user that owns the seller profile.</param>
    /// <param name="isActive"><see langword="true"/> to activate; <see langword="false"/> to deactivate.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Success when the status update is persisted; otherwise a failure for unauthorized callers or missing profile.
    /// </returns>
    Task<Result> ChangeStatus(
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the supplied user currently has an active seller profile.
    /// </summary>
    /// <param name="userId">Identifier of the user to check.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> when an active seller profile exists for the user; otherwise <see langword="false"/>.
    /// </returns>
    Task<bool> HasActiveProfile(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the active seller profile for the supplied user id.
    /// </summary>
    /// <param name="userId">Identifier of the user that owns the seller profile.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Active seller profile when one exists; otherwise <see langword="null"/>.
    /// </returns>
    Task<SellerProfileServiceModel?> ActiveByUserId(
        string userId,
        CancellationToken cancellationToken = default);
}
