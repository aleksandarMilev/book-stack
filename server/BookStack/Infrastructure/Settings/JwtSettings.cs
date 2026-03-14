namespace BookStack.Infrastructure.Settings;

public class JwtSettings
{
    public string Secret { get; init; } = null!;

    public string Issuer { get; init; } = "BookStack";

    public string Audience { get; init; } = "BookStackClient";
}
