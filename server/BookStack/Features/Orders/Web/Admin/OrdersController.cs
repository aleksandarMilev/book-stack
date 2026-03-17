namespace BookStack.Features.Orders.Web.Admin;

using Areas.Admin.Web;
using Shared;
using Common;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.Models;
using Web.Models;

public class OrdersController(IOrderService service) : AdminApiController
{
    private readonly IOrderService _service = service;

    [HttpGet]
    public async Task<ActionResult<PaginatedModel<OrderServiceModel>>> All(
        [FromQuery] OrderFilterWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToFilterServiceModel();
        var result = await this._service.All(
            serviceModel,
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet(Common.Constants.ApiRoutes.Id)]
    public async Task<ActionResult<OrderServiceModel?>> Details(
        Guid id,
        CancellationToken cancellationToken = default)
        => await this._service.Details(
            id,
            cancellationToken);

    [HttpPut(ApiRoutes.Status)]
    public async Task<ActionResult> ChangeStatus(
        Guid id,
        ChangeOrderStatusWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var result = await this._service.ChangeStatus(
            id,
            webModel.Status,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }

    [HttpPut(ApiRoutes.PaymentStatus)]
    public async Task<ActionResult> ChangePaymentStatus(
        Guid id,
        ChangePaymentStatusWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var result = await this._service.ChangePaymentStatus(
            id,
            webModel.PaymentStatus,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }

    [HttpPut(ApiRoutes.SettlementStatus)]
    public async Task<ActionResult> ChangeSettlementStatus(
        Guid id,
        ChangeSettlementStatusWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var result = await this._service.ChangeSettlementStatus(
            id,
            webModel.SettlementStatus,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }
}
