namespace BookStack.Features.SellerProfiles.Web.Admin;

using Areas.Admin.Web;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;

public class SellerProfilesController(ISellerProfileService service) : AdminApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SellerProfileServiceModel>>> All(
        CancellationToken cancellationToken = default)
        => this.Ok(await service.All(cancellationToken));

    [HttpGet(Common.Constants.ApiRoutes.Id)]
    public async Task<ActionResult<SellerProfileServiceModel>> ByUserId(
        string id,
        CancellationToken cancellationToken = default)
        => this.Ok(await service.ByUserId(id, cancellationToken));

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
