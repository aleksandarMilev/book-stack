namespace BookStack.Features.UserProfile.Service;

using Infrastructure.Services.Result;
using Infrastructure.Services.ServiceLifetimes;
using Models;

/// <summary>
/// Provides operations for reading and managing user profiles.
/// </summary>
public interface IProfileService : IScopedService
{
    /// <summary>
    /// Returns the profile of the current authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// The current user's profile when visible through query filters; otherwise <see langword="null"/>.
    /// </returns>
    Task<ProfileServiceModel?> Mine(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the initial profile for a user in a trusted internal flow.
    /// </summary>
    /// <param name="model">Profile data to persist.</param>
    /// <param name="userId">Identifier of the user that will own the profile.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The created profile.</returns>
    /// <remarks>
    /// This method is intended for internal orchestration such as account registration.
    /// </remarks>
    Task<ProfileServiceModel> Create(
        CreateProfileServiceModel model,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current authenticated user's profile.
    /// </summary>
    /// <param name="model">Updated profile data, including optional image remove/replace intent.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A success result when the update completes; otherwise a failure result describing why it was rejected.
    /// </returns>
    Task<Result> Edit(
        CreateProfileServiceModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a profile and the linked user according to authorization rules.
    /// </summary>
    /// <param name="userId">
    /// Optional target user id. When <see langword="null"/>, deletes the current authenticated user's own profile.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A success result when both profile and user soft-delete updates succeed; otherwise a failure result.
    /// </returns>
    Task<Result> Delete(
        string? userId = null,
        CancellationToken cancellationToken = default);
}
