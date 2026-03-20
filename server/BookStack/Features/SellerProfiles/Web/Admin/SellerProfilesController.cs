namespace BookStack.Features.SellerProfiles.Web.Admin;

using Areas.Admin.Web;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;

/// <summary>
/// Exposes administrator-only seller-profile management endpoints.
/// </summary>
public class SellerProfilesController(ISellerProfileService service) : AdminApiController
{
    /// <summary>
    /// Returns all visible seller profiles.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>`200 OK` with seller profiles ordered by newest first.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SellerProfileServiceModel>>> All(
        CancellationToken cancellationToken = default)
        => this.Ok(await service.All(cancellationToken));

    /// <summary>
    /// Returns a seller profile by target user id.
    /// </summary>
    /// <param name="id">Identifier of the user that owns the seller profile.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// `200 OK` with seller-profile payload, or `200 OK` with <see langword="null"/> when no visible profile exists.
    /// </returns>
    [HttpGet(Common.Constants.ApiRoutes.Id)]
    public async Task<ActionResult<SellerProfileServiceModel>> ByUserId(
        string id,
        CancellationToken cancellationToken = default)
        => this.Ok(await service.ByUserId(id, cancellationToken));

    /// <summary>
    /// Changes activation status for a target seller profile.
    /// </summary>
    /// <param name="id">Identifier of the user that owns the seller profile.</param>
    /// <param name="webModel">Requested activation state.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// `204 No Content` when status change succeeds, or `400 Bad Request` when service validation/business checks fail.
    /// </returns>
    [HttpPut(ApiRoutes.Status)]
    public async Task<ActionResult> ChangeStatus(
        string id,
        ChangeSellerProfileStatusWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ChangeStatus(
            id,
            webModel.IsActive,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }
}
