namespace BookStack.Tests.Features.BookListings;

using BookStack.Features.BookListings.Service.Models;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

public class SellerProfileListingRulesTests
{
    [Fact]
    public async Task Create_FailsWithoutActiveSellerProfile()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-1",
            Username = "seller-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 12, 0, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: currentUserService.UserId!,
            title: "Seller Rules Book",
            author: "Seller Rules Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var result = await service.Create(
            CreateListingServiceModel(book.Id),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "active seller profile",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_SucceedsWithActiveSellerProfile()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-2",
            Username = "seller-2",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 13, 0, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        await EnsureActiveSellerProfile(
            data,
            currentUserService.UserId!);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: currentUserService.UserId!,
            title: "Seller Rules Book 2",
            author: "Seller Rules Author 2");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var result = await service.Create(
            CreateListingServiceModel(book.Id),
            CancellationToken.None);

        var createdListing = await data
            .BookListings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                l => l.Id == result.Data,
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(createdListing);
    }

    [Fact]
    public async Task Edit_FailsWithoutActiveSellerProfile()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-3",
            Username = "seller-3",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 14, 0, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        await EnsureActiveSellerProfile(
            data,
            currentUserService.UserId!);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: currentUserService.UserId!,
            title: "Editable Listing Book",
            author: "Editable Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            bookId: book.Id,
            creatorId: currentUserService.UserId!,
            isApproved: true);

        data.BookListings.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        var sellerProfile = await data
            .SellerProfiles
            .SingleAsync(
                p => p.UserId == currentUserService.UserId,
                CancellationToken.None);

        sellerProfile.IsActive = false;
        await data.SaveChangesAsync(CancellationToken.None);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        var result = await service.Edit(
            listing.Id,
            CreateListingServiceModel(book.Id),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "active seller profile",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_FailsWhenCurrencyIsNotEur()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-4",
            Username = "seller-4",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 14, 12, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        await EnsureActiveSellerProfile(
            data,
            currentUserService.UserId!);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: currentUserService.UserId!,
            title: "Non-EUR Listing Book",
            author: "Non-EUR Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var result = await service.Create(
            CreateListingServiceModel(
                book.Id,
                currency: "USD"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "EUR",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Edit_FailsWhenCurrencyIsNotEur()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-5",
            Username = "seller-5",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 14, 18, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        await EnsureActiveSellerProfile(
            data,
            currentUserService.UserId!);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: currentUserService.UserId!,
            title: "Editable Currency Book",
            author: "Editable Currency Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            bookId: book.Id,
            creatorId: currentUserService.UserId!,
            isApproved: true,
            currency: "EUR");

        data.BookListings.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        var result = await service.Edit(
            listing.Id,
            CreateListingServiceModel(
                book.Id,
                currency: "BGN"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "EUR",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    private static CreateBookListingServiceModel CreateListingServiceModel(
        Guid bookId,
        string currency = "EUR")
        => new()
        {
            BookId = bookId,
            Price = 22.50m,
            Currency = currency,
            Condition = BookStack.Features.BookListings.Shared.ListingCondition.LikeNew,
            Quantity = 3,
            Description = "Seller profile listing test description",
            Image = null,
            RemoveImage = false,
        };

    private static async Task EnsureActiveSellerProfile(
        BookStack.Data.BookStackDbContext data,
        string sellerId)
    {
        var userExists = await data
            .Users
            .AnyAsync(u => u.Id == sellerId);

        if (!userExists)
        {
            data.Users.Add(MarketplaceTestData.CreateUser(
                sellerId,
                $"{sellerId}@example.com"));
        }

        var profileExists = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .AnyAsync(p => p.UserId == sellerId);

        if (!profileExists)
        {
            data.SellerProfiles.Add(MarketplaceTestData.CreateSellerProfile(sellerId));
        }

        await data.SaveChangesAsync(CancellationToken.None);
    }
}
