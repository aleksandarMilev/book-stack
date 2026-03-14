namespace BookStack.Infrastructure.Services.CurrentUser;

using ServiceLifetimes;

public interface ICurrentUserService : IScopedService
{
    string? GetUsername();

    string? GetId();

    bool IsAdmin();
}
