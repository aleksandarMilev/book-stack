namespace BookStack.Features.Identity.Service.Models;

/// <summary>
/// API response model containing a JWT token.
/// </summary>
/// <param name="token">Serialized JWT token string.</param>
public class JwtTokenServiceModel(string token)
{
    /// <summary>
    /// Serialized JWT token string.
    /// </summary>
    public string Token { get; init; } = token;
}
