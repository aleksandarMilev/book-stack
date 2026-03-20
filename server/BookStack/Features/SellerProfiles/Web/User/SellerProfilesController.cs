namespace BookStack.Features.SellerProfiles.Web.User;

using Common;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;
using Shared;

/// <summary>
/// Exposes authenticated-user seller-profile endpoints.
/// </summary>
/// <remarks>
/// This controller maps web models to service models and delegates seller capability rules to
/// <see cref="ISellerProfileService"/>.
/// </remarks>
[Authorize]
public class SellerProfilesController(ISellerProfileService service) : ApiController
{
    /// <summary>
    /// Returns the current authenticated user's seller profile.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// `200 OK` with seller-profile payload, or `200 OK` with <see langword="null"/> when no visible seller profile exists.
    /// </returns>
    [HttpGet(ApiRoutes.Mine)]
    public async Task<ActionResult<SellerProfileServiceModel>> Mine(
        CancellationToken cancellationToken = default)
        => this.Ok(await service.Mine(cancellationToken));

    /// <summary>
    /// Creates or updates the current authenticated user's seller profile.
    /// </summary>
    /// <param name="webModel">Editable seller-profile payload.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// `200 OK` with seller-profile payload when upsert succeeds, or `400 Bad Request` when service business checks fail.
    /// </returns>
    [HttpPut(ApiRoutes.Mine)]
    public async Task<ActionResult<SellerProfileServiceModel>> UpsertMine(
        UpsertSellerProfileWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToUpsertServiceModel();
        var result = await service.UpsertMine(
            serviceModel,
            cancellationToken);

        return this.OkOrBadRequest(
            result,
            static profile => profile);
    }
}
