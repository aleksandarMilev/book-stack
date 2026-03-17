namespace BookStack.Tests.Features.Privacy;

using BookStack.Data;
using BookStack.Features.BookListings.Service.Models;
using BookStack.Features.Orders.Service.Models;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

public class PublicDtoPrivacyTests
{
    [Fact]
    public async Task PublicBookResponses_DoNotExposeModerationOrCreatorInternals()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();
        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 14, 0, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var bookService = TestServiceFactory.CreateBookService(
            data,
            currentUserService,
            dateTimeProvider);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: "seller-1",
            title: "Privacy Book",
            author: "Privacy Author",
            isApproved: true);

        book.ApprovedBy = "admin-1";
        book.ApprovedOn = dateTimeProvider.UtcNow;
        book.RejectionReason = "Should be hidden";

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var result = await bookService.All(new(), CancellationToken.None);
        var model = Assert.Single(result.Items);

        Assert.Equal(string.Empty, model.CreatorId);
        Assert.Null(model.ApprovedBy);
        Assert.Null(model.ApprovedOn);
        Assert.Null(model.RejectionReason);
    }

    [Fact]
    public async Task PublicListingResponses_DoNotExposeModerationOrCreatorInternals()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();
        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 14, 1, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var listingService = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        await EnsureActiveSellerProfile(data, "seller-1");

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: "seller-1",
            title: "Listing Privacy Book",
            author: "Listing Privacy Author",
            isApproved: true);

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            creatorId: "seller-1",
            isApproved: true);

        listing.ApprovedBy = "admin-1";
        listing.ApprovedOn = dateTimeProvider.UtcNow;
        listing.RejectionReason = "Hidden reason";

        data.BookListings.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        var result = await listingService.All(new(), CancellationToken.None);
        var model = Assert.Single(result.Items);

        Assert.Equal(string.Empty, model.CreatorId);
        Assert.Null(model.ApprovedBy);
        Assert.Null(model.ApprovedOn);
        Assert.Null(model.RejectionReason);
    }

    [Fact]
    public void SellerAndBuyerOrderDtos_DoNotExposeProviderInternals()
    {
        var sellerPropertyNames = typeof(SellerOrderServiceModel)
            .GetProperties()
            .Select(static p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var buyerPropertyNames = typeof(OrderServiceModel)
            .GetProperties()
            .Select(static p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("ProviderPaymentId", sellerPropertyNames);
        Assert.DoesNotContain("GuestPaymentTokenHash", sellerPropertyNames);
        Assert.DoesNotContain("ProviderPaymentId", buyerPropertyNames);
        Assert.DoesNotContain("GuestPaymentTokenHash", buyerPropertyNames);
    }

    private static async Task EnsureActiveSellerProfile(
        BookStackDbContext data,
        string sellerId)
    {
        var userExists = await data.Users.AnyAsync(u => u.Id == sellerId);
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
