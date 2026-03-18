namespace BookStack.Tests.Data.Seeders.Listings;

using BookStack.Areas.Admin.Service;
using BookStack.Data.Seeders.Listings;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

public class ListingsSeederTests
{
    [Fact]
    public async Task Seed_UsesOfficialEurCurrencyForSeededListings()
    {
        await using var database = new TestDatabaseScope();

        var currentUserService = new TestCurrentUserService
        {
            UserId = "admin-1",
            Username = "admin-1",
            Admin = true,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        data.Users.AddRange(
            MarketplaceTestData.CreateUser("seller-1", "seller-1@example.com"),
            MarketplaceTestData.CreateUser("admin-1", "admin-1@example.com"));

        data.SellerProfiles.Add(
            MarketplaceTestData.CreateSellerProfile("seller-1", isActive: true));

        data.Books.Add(
            MarketplaceTestData.CreateApprovedBook(
                creatorId: "seller-1",
                title: "Seed Book",
                author: "Seed Author",
                isApproved: true));

        await data.SaveChangesAsync(CancellationToken.None);

        var seeder = new ListingsSeeder(
            data,
            new FakeAdminService("admin-1"),
            dateTimeProvider,
            NullLogger<ListingsSeeder>.Instance);

        await seeder.Seed(CancellationToken.None);

        var listings = await data
            .BookListings
            .AsNoTracking()
            .ToListAsync(CancellationToken.None);

        Assert.NotEmpty(listings);
        Assert.All(
            listings,
            static listing => Assert.Equal("EUR", listing.Currency));
    }

    private sealed class FakeAdminService(string adminId) : IAdminService
    {
        public Task<string> GetId()
            => Task.FromResult(adminId);
    }
}
