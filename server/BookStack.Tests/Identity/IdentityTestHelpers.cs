namespace BookStack.Tests.Identity;

using System.Text;
using Microsoft.AspNetCore.WebUtilities;

internal static class IdentityTestHelpers
{
    public static string EncodeIdentityToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        return WebEncoders.Base64UrlEncode(tokenBytes);
    }
}
