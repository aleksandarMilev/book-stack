namespace BookStack.Features.Books.Web.User;

using Common;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;
using Shared;

[Authorize]
public class BooksController(IBookService service) : ApiController
{
    private readonly IBookService _service = service;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PaginatedModel<BookServiceModel>>> All(
        [FromQuery] BookFilterWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToFilterServiceModel();
        var result = await this._service.All(
            serviceModel,
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet(ApiRoutes.Mine)]
    public async Task<ActionResult<PaginatedModel<BookServiceModel>>> Mine(
        [FromQuery] BookFilterWebModel webModel,
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
    public async Task<ActionResult<BookServiceModel>> Details(
        Guid id,
        CancellationToken cancellationToken = default)
        => this.Ok(await this._service.Details(id, cancellationToken));

    [AllowAnonymous]
    [HttpGet(ApiRoutes.Lookup)]
    public async Task<ActionResult<IEnumerable<BookLookupServiceModel>>> Lookup(
        [FromQuery] string? query,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
        => this.Ok(await this._service.Lookup(query, take, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        CreateBookWebModel webModel,
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
        CreateBookWebModel webModel,
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