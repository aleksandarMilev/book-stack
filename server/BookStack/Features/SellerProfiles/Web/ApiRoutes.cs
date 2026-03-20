namespace BookStack.Features.SellerProfiles.Web;

/// <summary>
/// Defines route segments for seller-profile API actions.
/// </summary>
public static class ApiRoutes
{
    /// <summary>
    /// Route segment for current authenticated user's seller profile.
    /// </summary>
    public const string Mine = "mine/";

    /// <summary>
    /// Route segment for administrator seller-status changes by user id.
    /// </summary>
    public const string Status = "{id}/status/";
}
