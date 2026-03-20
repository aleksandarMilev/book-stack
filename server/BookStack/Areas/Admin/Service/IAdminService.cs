namespace BookStack.Areas.Admin.Service;

using Infrastructure.Services.ServiceLifetimes;

public interface IAdminService : IScopedService
{
    Task<IEnumerable<string>> GetIds();
}
