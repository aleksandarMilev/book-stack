namespace BookStack.Infrastructure.Services.CurrentUser;

using System.Security.Claims;
using Extensions;

using static Common.Constants.Names;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? CurrnetUser
        => this._httpContextAccessor.HttpContext?.User;

    public string? GetUsername()
        => this.CurrnetUser?.Identity?.Name;

    public string? GetId()
        => this.CurrnetUser?.GetId();

    public bool IsAdmin()
        => this.CurrnetUser?.IsInRole(AdminRoleName) ?? false;
}
