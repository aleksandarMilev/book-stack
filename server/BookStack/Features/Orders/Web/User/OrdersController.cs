namespace BookStack.Features.Orders.Web.User;

using Common;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;
using Shared;

[ApiController]
[Route("[controller]")]
public class OrdersController(IOrderService service) : ControllerBase
{
    private readonly IOrderService _service = service;

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<CreateOrderResultWebModel>> Create(
        CreateOrderWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToCreateServiceModel();
        var result = await this._service.Create(
            serviceModel,
            cancellationToken);

        return this.OkOrBadRequest(
            result,
            static createResult => createResult.ToWebModel());
    }

    [Authorize]
    [HttpGet(ApiRoutes.Mine)]
    public async Task<ActionResult<PaginatedModel<OrderServiceModel>>> Mine(
        [FromQuery] OrderFilterWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToFilterServiceModel();
        var result = await this._service.Mine(
            serviceModel,
            cancellationToken);

        return this.Ok(result);
    }

    [Authorize]
    [HttpGet(ApiRoutes.Sold)]
    public async Task<ActionResult<PaginatedModel<SellerOrderServiceModel>>> Sold(
        [FromQuery] OrderFilterWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToFilterServiceModel();
        var result = await this._service.Sold(
            serviceModel,
            cancellationToken);

        return this.Ok(result);
    }

    [Authorize]
    [HttpGet(Common.Constants.ApiRoutes.Id)]
    public async Task<ActionResult<OrderServiceModel>> Details(
        Guid id,
        CancellationToken cancellationToken = default)
        => this.Ok(await this._service.Details(id, cancellationToken));

    [Authorize]
    [HttpGet($"{ApiRoutes.Sold}{Common.Constants.ApiRoutes.Id}")]
    public async Task<ActionResult<SellerOrderServiceModel>> SoldDetails(
        Guid id,
        CancellationToken cancellationToken = default)
        => this.Ok(await this._service.SoldDetails(id, cancellationToken));

    [Authorize]
    [HttpPut(ApiRoutes.SoldConfirm)]
    public async Task<ActionResult> ConfirmSoldOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await this._service.ConfirmSoldOrder(
            id,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }

    [Authorize]
    [HttpPut(ApiRoutes.SoldShip)]
    public async Task<ActionResult> ShipSoldOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await this._service.ShipSoldOrder(
            id,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }

    [Authorize]
    [HttpPut(ApiRoutes.SoldDeliver)]
    public async Task<ActionResult> DeliverSoldOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await this._service.DeliverSoldOrder(
            id,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }
}
