namespace BookStack.Tests.Features.Security;

using BookStack.Data;
using BookStack.Features.BookListings.Service;
using BookStack.Features.Books.Service;
using BookStack.Features.Orders.Shared;
using BookStack.Features.SellerProfiles.Service;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

public class RoleBoundaryHardeningTests
{
    [Fact]
    public async Task NonAdmin_CannotApproveBook()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
            Admin = false,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 13, 0, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookService(
            data,
            currentUserService,
            dateTimeProvider);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: "seller-1",
            title: "Moderation Guard Book",
            author: "Author",
            isApproved: false);

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var result = await service.Approve(book.Id, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("administrators", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NonAdmin_CannotApproveListing()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
            Admin = false,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 13, 1, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        await EnsureActiveSellerProfile(data, "seller-1");

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: "seller-1",
            title: "Listing Moderation Guard Book",
            author: "Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            creatorId: "seller-1",
            isApproved: false);

        data.BookListings.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        var result = await service.Approve(listing.Id, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("administrators", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NonAdmin_CannotApplyManualPaymentStatusOverride()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
            Admin = false,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 13, 2, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(
            data,
            dateTimeProvider,
            currentUserService);

        var orderService = TestServiceFactory.CreateOrderService(
            data,
            currentUserService,
            paymentService,
            dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1");

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var result = await paymentService.ApplyManualPaymentStatus(
            createResult.Data!.OrderId,
            PaymentStatus.Paid,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("administrators", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NonAdmin_CannotChangeSellerProfileStatus_OrReadAllProfiles()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
            Admin = false,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 13, 3, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        await EnsureActiveSellerProfile(data, "seller-1");

        var service = new SellerProfileService(
            data,
            currentUserService,
            NullLogger<SellerProfileService>.Instance);

        var allProfiles = await service.All(CancellationToken.None);
        var byUserId = await service.ByUserId("seller-1", CancellationToken.None);
        var changeResult = await service.ChangeStatus("seller-1", false, CancellationToken.None);

        Assert.Empty(allProfiles);
        Assert.Null(byUserId);
        Assert.False(changeResult.Succeeded);
        Assert.Contains("administrators", changeResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<BookStack.Features.BookListings.Data.Models.BookListingDbModel> SeedApprovedListing(
        BookStackDbContext data,
        string sellerId)
    {
        await EnsureActiveSellerProfile(data, sellerId);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: sellerId,
            title: "Role Boundary Book",
            author: "Role Boundary Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            creatorId: sellerId,
            quantity: 5);

        data.BookListings.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        return listing;
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
