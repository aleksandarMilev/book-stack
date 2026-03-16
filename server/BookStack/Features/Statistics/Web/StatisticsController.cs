namespace BookStack.Features.Statistics.Web;

using Areas.Admin.Web;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.Models;

public class StatisticsController(
    IStatisticsService service) : AdminApiController
{
    private readonly IStatisticsService _service = service;

    [HttpGet]
    public async Task<ActionResult<AdminStatisticsServiceModel>> Get(
        CancellationToken cancellationToken = default)
        => this.Ok(await this._service.Get(cancellationToken));
}
