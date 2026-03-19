namespace BookStack.Tests.Identity;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

internal static class IdentityTestHelpers
{
    public static string EncodeIdentityToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        return WebEncoders.Base64UrlEncode(tokenBytes);
    }

    public static JwtSecurityToken ReadJwtToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(token);
    }

    public static string? GetClaimValue(
        JwtSecurityToken token,
        string claimType)
        => token.Claims
            .SingleOrDefault(c => c.Type == claimType)
            ?.Value;

    public static string? GetClaimValue(
        JwtSecurityToken token,
        Claim claim)
        => GetClaimValue(token, claim.Type);
}
