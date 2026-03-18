namespace BookStack.Features.Identity.Service.Models;

/// <summary>
/// Service-layer payload for login requests.
/// </summary>
public class LoginServiceModel
{
    /// <summary>
    /// Username or email used to resolve the account.
    /// </summary>
    public string Credentials { get; init; } = default!;

    /// <summary>
    /// Indicates whether token expiration should be extended.
    /// </summary>
    public bool RememberMe { get; init; }

    /// <summary>
    /// Raw password used to authenticate the user.
    /// </summary>
    public string Password { get; init; } = default!;
}
