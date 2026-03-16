namespace BookStack.Features.Orders.Shared;

using System.Security.Cryptography;
using System.Text;

public static class OrderPaymentToken
{
    private const int TokenBytesLength = 32;

    public static string Generate()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytesLength));

    public static string Hash(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    public static bool Verify(
        string token,
        string hash)
    {
        var tokenHash = Hash(token);
        var tokenHashBytes = Encoding.UTF8.GetBytes(tokenHash);
        var storedHashBytes = Encoding.UTF8.GetBytes(hash);

        return CryptographicOperations.FixedTimeEquals(
            tokenHashBytes,
            storedHashBytes);
    }
}
