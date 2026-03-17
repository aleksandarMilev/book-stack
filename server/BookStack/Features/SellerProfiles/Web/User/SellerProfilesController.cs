namespace BookStack.Features.SellerProfiles.Web.User;

using Common;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;
using Shared;

[Authorize]
public class SellerProfilesController(
    ISellerProfileService service) : ApiController
{
    private readonly ISellerProfileService _service = service;

    [HttpGet(ApiRoutes.Mine)]
    public async Task<ActionResult<SellerProfileServiceModel>> Mine(
        CancellationToken cancellationToken = default)
        => this.Ok(await this._service.Mine(cancellationToken));

    [HttpPut(ApiRoutes.Mine)]
    public async Task<ActionResult<SellerProfileServiceModel>> UpsertMine(
        UpsertSellerProfileWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToUpsertServiceModel();
        var result = await this._service.UpsertMine(
            serviceModel,
            cancellationToken);

        return this.OkOrBadRequest(
            result,
            static profile => profile);
    }
}
