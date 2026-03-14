namespace BookStack.Features.UserProfile.Web.Admin;

using Areas.Admin.Web;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Service;

using static Common.Constants.ApiRoutes;

public class ProfilesController(IProfileService service) : AdminApiController
{
    [HttpDelete(Id)]
    public async Task<ActionResult> Delete(
        string id,
        CancellationToken cancellationToken = default)
    {
        var result = await service.Delete(
            id,
            cancellationToken);

        return this.NoContentOrBadRequest(result);
    }
}
