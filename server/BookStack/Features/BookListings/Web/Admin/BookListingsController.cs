namespace BookStack.Features.BookListings.Web.Admin;

using Areas.Admin.Web;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Service;
using Web.Models;

using static Common.Constants.ApiRoutes;

public class BookListingsController(IBookListingService service) : AdminApiController
{
    private readonly IBookListingService _service = service;

    [HttpPost(ApiRoutes.Approve)]
    public async Task<ActionResult> Approve(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await this._service.Approve(
            id,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }

    [HttpPost(ApiRoutes.Reject)]
    public async Task<ActionResult> Reject(
        Guid id,
        RejectBookListingWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var result = await this._service.Reject(
            id,
            webModel.RejectionReason,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }

    [HttpDelete(Id)]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await this._service.Delete(
            id,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }
}
