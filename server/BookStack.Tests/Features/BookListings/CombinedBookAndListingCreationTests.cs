namespace BookStack.Tests.Features.BookListings;

using BookStack.Features.BookListings.Service.Models;
using BookStack.Infrastructure.Services.ImageWriter;
using BookStack.Infrastructure.Services.ImageWriter.Models;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

public class CombinedBookAndListingCreationTests
{
    [Fact]
    public async Task CreateWithBook_SucceedsAndCreatesBothEntitiesInPendingState()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-combined-1",
            Username = "seller-combined-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 10, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        await EnsureActiveSellerProfile(
            data,
            currentUserService.UserId!);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        var result = await service.CreateWithBook(
            CreateCombinedModel(
                title: "Atomic Book One",
                author: "Atomic Author One"),
            CancellationToken.None);

        var listing = await data
            .BookListings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                l => l.Id == result.Data,
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(listing);
        Assert.False(listing!.IsApproved);

        var book = await data
            .Books
            .AsNoTracking()
            .SingleOrDefaultAsync(
                b => b.Id == listing.BookId,
                CancellationToken.None);

        Assert.NotNull(book);
        Assert.False(book!.IsApproved);
        Assert.Equal(book.Id, listing.BookId);
    }

    [Fact]
    public async Task CreateWithBook_WhenListingCreationThrows_RollsBackCreatedBook()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-combined-2",
            Username = "seller-combined-2",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 11, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        await EnsureActiveSellerProfile(
            data,
            currentUserService.UserId!);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new ThrowingImageWriter());

        var title = "Atomic Rollback Book";
        var author = "Atomic Rollback Author";

        var result = await service.CreateWithBook(
            CreateCombinedModel(
                title: title,
                author: author),
            CancellationToken.None);

        Assert.False(result.Succeeded);

        var persistedBook = await data
            .Books
            .AsNoTracking()
            .SingleOrDefaultAsync(
                b => b.Title == title && b.Author == author,
                CancellationToken.None);

        var persistedListingsCount = await data
            .BookListings
            .AsNoTracking()
            .CountAsync(CancellationToken.None);

        Assert.Null(persistedBook);
        Assert.Equal(0, persistedListingsCount);
    }

    [Fact]
    public async Task CreateWithBook_InvalidCombinedRequest_DoesNotPersistPartialData()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-combined-3",
            Username = "seller-combined-3",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 12, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        await EnsureActiveSellerProfile(
            data,
            currentUserService.UserId!);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        var title = "Atomic Invalid Currency Book";
        var author = "Atomic Invalid Currency Author";

        var result = await service.CreateWithBook(
            CreateCombinedModel(
                title: title,
                author: author,
                currency: "USD"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "EUR",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);

        var persistedBook = await data
            .Books
            .AsNoTracking()
            .SingleOrDefaultAsync(
                b => b.Title == title && b.Author == author,
                CancellationToken.None);

        var persistedListingsCount = await data
            .BookListings
            .AsNoTracking()
            .CountAsync(CancellationToken.None);

        Assert.Null(persistedBook);
        Assert.Equal(0, persistedListingsCount);
    }

    [Fact]
    public async Task CreateWithBook_FailsWhenSellerHasNoActiveProfile()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-combined-4",
            Username = "seller-combined-4",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 13, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        var result = await service.CreateWithBook(
            CreateCombinedModel(
                title: "No Profile Book",
                author: "No Profile Author"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "active seller profile",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);

        var booksCount = await data
            .Books
            .AsNoTracking()
            .CountAsync(CancellationToken.None);

        var listingsCount = await data
            .BookListings
            .AsNoTracking()
            .CountAsync(CancellationToken.None);

        Assert.Equal(0, booksCount);
        Assert.Equal(0, listingsCount);
    }

    private static CreateBookListingWithBookServiceModel CreateCombinedModel(
        string title,
        string author,
        string currency = "EUR")
        => new()
        {
            Book = new()
            {
                Title = title,
                Author = author,
                Genre = "Fantasy",
                Description = "Combined create canonical book description.",
                Publisher = "Combined Publisher",
                PublishedOn = new DateOnly(2020, 01, 01),
                Isbn = null,
            },
            Price = 17.90m,
            Currency = currency,
            Condition = BookStack.Features.BookListings.Shared.ListingCondition.Good,
            Quantity = 2,
            Description = "Combined create listing description for moderation.",
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

    private sealed class ThrowingImageWriter : IImageWriter
    {
        public Task Write(
            string resourceName,
            IImageDdModel dbModel,
            IImageServiceModel serviceModel,
            string? defaultImagePath = null,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated listing image write failure.");

        public bool Delete(
            string resourceName,
            string? imagePath,
            string? defaultImagePath = null)
            => true;
    }
}
