namespace BookStack.Features.BookListings.Web.User;

using Common;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;
using Shared;

[Authorize]
public class BookListingsController(IBookListingService service) : ApiController
{
    private readonly IBookListingService _service = service;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PaginatedModel<BookListingServiceModel>>> All(
        [FromQuery] BookListingFilterWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToFilterServiceModel();
        var result = await this._service.All(
            serviceModel,
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet(ApiRoutes.Mine)]
    public async Task<ActionResult<PaginatedModel<BookListingServiceModel>>> Mine(
        [FromQuery] BookListingFilterWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToFilterServiceModel();
        var result = await this._service.Mine(
            serviceModel,
            cancellationToken);

        return this.Ok(result);
    }

    [AllowAnonymous]
    [HttpGet(Common.Constants.ApiRoutes.Id)]
    public async Task<ActionResult<BookListingServiceModel>> Details(
        Guid id,
        CancellationToken cancellationToken = default)
        => this.Ok(await this._service.Details(id, cancellationToken));

    [AllowAnonymous]
    [HttpGet(ApiRoutes.Lookup)]
    public async Task<ActionResult<IEnumerable<BookListingLookupServiceModel>>> Lookup(
        [FromQuery] string? query,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
        => this.Ok(await this._service.Lookup(query, take, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromForm] CreateBookListingWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToCreateServiceModel();
        var result = await this._service.Create(
            serviceModel,
            cancellationToken);

        return this.OkOrBadRequest(
            result,
            static id => id);
    }

    [HttpPut(Common.Constants.ApiRoutes.Id)]
    public async Task<ActionResult> Edit(
        Guid id,
        [FromForm] CreateBookListingWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToCreateServiceModel();
        var result = await this._service.Edit(
            id,
            serviceModel,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }

    [HttpDelete(Common.Constants.ApiRoutes.Id)]
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
