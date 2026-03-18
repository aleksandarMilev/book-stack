namespace BookStack.Features.Identity.Web;

/// <summary>
/// Defines route segments for identity API actions.
/// </summary>
public class ApiRoutes
{
    /// <summary>
    /// Route for user registration.
    /// </summary>
    public const string RegisterRoute = "register/";

    /// <summary>
    /// Route for user login.
    /// </summary>
    public const string LoginRoute = "login/";

    /// <summary>
    /// Route for requesting a password reset link.
    /// </summary>
    public const string ForgotPasswordRoute = "forgot-password/";

    /// <summary>
    /// Route for resetting a password with a valid reset token.
    /// </summary>
    public const string ResetPasswordRoute = "reset-password/";
}
