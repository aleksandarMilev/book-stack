namespace BookStack.Data.Seeders.Books;

using Infrastructure.Services.ServiceLifetimes;

public interface IBookSeeder : IScopedService
{
    Task Seed(CancellationToken cancellationToken);
}
