namespace BookStack.Features.UserProfile.Web.User;

using Common;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;
using Shared;

/// <summary>
/// Exposes authenticated-user profile endpoints.
/// </summary>
/// <remarks>
/// This controller maps web models to service models and delegates business rules to
/// <see cref="IProfileService"/>.
/// </remarks>
[Authorize]
public class ProfilesController(IProfileService service) : ApiController
{
    /// <summary>
    /// Returns the current authenticated user's profile.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// `200 OK` with the current profile payload, or `200 OK` with <see langword="null"/> when no visible profile exists.
    /// </returns>
    [HttpGet(
        ApiRoutes.Mine,
        Name = nameof(Mine))]
    public async Task<ActionResult<ProfileServiceModel>> Mine(
        CancellationToken cancellationToken = default)
        => this.Ok(await service.Mine(cancellationToken));

    /// <summary>
    /// Updates the current authenticated user's profile.
    /// </summary>
    /// <param name="webModel">Editable profile payload, including optional image upload or remove-image intent.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// `204 No Content` when the update succeeds, or `400 Bad Request` when service validation/business checks fail.
    /// </returns>
    /// <remarks>
    /// The request is bound from form data because profile editing supports file upload.
    /// </remarks>
    [HttpPut]
    public async Task<ActionResult> Edit(
        [FromForm]
        CreateProfileWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToCreateServiceModel();
        var result = await service.Edit(
            serviceModel,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }

    /// <summary>
    /// Soft-deletes the current authenticated user's profile.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// `204 No Content` when deletion succeeds, or `400 Bad Request` when service validation/business checks fail.
    /// </returns>
    [HttpDelete]
    public async Task<ActionResult> Delete(
        CancellationToken cancellationToken = default)
    {
        var result = await service.Delete(
            cancellationToken: cancellationToken);

        return this.NoContentOrBadRequest(result);
    }
}
