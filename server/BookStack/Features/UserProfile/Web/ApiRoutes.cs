namespace BookStack.Features.UserProfile.Web;

/// <summary>
/// Defines route segments for user-profile API actions.
/// </summary>
public static class ApiRoutes
{
    /// <summary>
    /// Route segment for top profiles queries.
    /// </summary>
    public const string Top = "top/";

    /// <summary>
    /// Route segment for profile existence checks.
    /// </summary>
    public const string Exists = "exists/";

    /// <summary>
    /// Route segment for the current authenticated user's profile.
    /// </summary>
    public const string Mine = "mine/";
}
