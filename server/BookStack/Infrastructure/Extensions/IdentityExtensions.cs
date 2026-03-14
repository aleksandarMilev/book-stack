namespace BookStack.Infrastructure.Extensions;

using System.Security.Claims;

public static class IdentityExtensions
{
    extension(ClaimsPrincipal user)
    {
        public string? GetId()
            => user
                .Claims
                .FirstOrDefault(static c => c.Type == ClaimTypes.NameIdentifier)
                ?.Value;
    }
}
