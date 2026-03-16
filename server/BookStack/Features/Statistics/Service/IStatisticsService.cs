namespace BookStack.Features.Statistics.Service;

using Infrastructure.Services.ServiceLifetimes;
using Models;

public interface IStatisticsService : IScopedService
{
    Task<AdminStatisticsServiceModel> Get(
        CancellationToken cancellationToken = default);
}
