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
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        CreateOrderWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToCreateServiceModel();
        var result = await service.Create(
            serviceModel,
            cancellationToken);

        return this.OkOrBadRequest(
            result,
            static id => id);
    }

    [Authorize]
    [HttpGet(ApiRoutes.Mine)]
    public async Task<ActionResult<PaginatedModel<OrderServiceModel>>> Mine(
        [FromQuery] OrderFilterWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToFilterServiceModel();
        var result = await service.Mine(
            serviceModel,
            cancellationToken);

        return this.Ok(result);
    }

    [Authorize]
    [HttpGet(Common.Constants.ApiRoutes.Id)]
    public async Task<ActionResult<OrderServiceModel>> Details(
        Guid id,
        CancellationToken cancellationToken = default)
        => this.Ok(await service.Details(id, cancellationToken));
}
