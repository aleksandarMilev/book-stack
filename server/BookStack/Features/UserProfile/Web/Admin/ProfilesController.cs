namespace BookStack.Features.UserProfile.Web.Admin;

using Areas.Admin.Web;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Service;

using static Common.Constants.ApiRoutes;

/// <summary>
/// Exposes administrator-only profile management endpoints.
/// </summary>
public class ProfilesController(IProfileService service) : AdminApiController
{
    /// <summary>
    /// Soft-deletes a target user's profile.
    /// </summary>
    /// <param name="id">Identifier of the user whose profile should be deleted.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// `204 No Content` when deletion succeeds, or `400 Bad Request` when service validation/business checks fail.
    /// </returns>
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
