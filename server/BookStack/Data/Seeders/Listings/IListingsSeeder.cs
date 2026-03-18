namespace BookStack.Data.Seeders.Listings;

using Infrastructure.Services.ServiceLifetimes;

public interface IListingsSeeder : IScopedService
{
    Task Seed(CancellationToken cancellationToken);
}
