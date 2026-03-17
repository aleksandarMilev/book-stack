namespace BookStack.Tests.Features.Payments;

using BookStack.Data;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Features.Orders.Shared;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

public class GuestPaymentTokenAuthorizationTests
{
    [Fact]
    public async Task GuestOrderCreation_ReturnsTokenAndStoresOnlyHash()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

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

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 1));

        var createResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var createdOrder = await data
            .Orders
            .SingleAsync(
                o => o.Id == createResult.Data!.OrderId,
                CancellationToken.None);

        var paymentToken = createResult.Data!.PaymentToken;

        Assert.True(createResult.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(paymentToken));
        Assert.False(string.IsNullOrWhiteSpace(createdOrder.GuestPaymentTokenHash));
        Assert.NotEqual(paymentToken, createdOrder.GuestPaymentTokenHash);
        Assert.True(
            OrderPaymentToken.Verify(
                paymentToken!,
                createdOrder.GuestPaymentTokenHash!));
    }

    [Fact]
    public async Task GuestCheckoutSession_SucceedsWithValidOrderIdAndToken()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 02, 02, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

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

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 1));

        var createResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var checkoutResult = await paymentService.CreateCheckoutSession(
            createResult.Data!.OrderId,
            model: new()
            {
                PaymentToken = createResult.Data.PaymentToken,
            },
            CancellationToken.None);

        Assert.True(checkoutResult.Succeeded);
        Assert.NotNull(checkoutResult.Data);
    }

    [Fact]
    public async Task GuestCheckoutSession_FailsWithInvalidToken()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 02, 03, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

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

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 1));

        var createResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var checkoutResult = await paymentService.CreateCheckoutSession(
            createResult.Data!.OrderId,
            model: new()
            {
                PaymentToken = "NOT-A-VALID-TOKEN",
            });

        Assert.False(checkoutResult.Succeeded);
        Assert.Contains(
            "not authorized",
            checkoutResult.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GuestCheckoutSession_FailsWhenTokenIsMissing()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 02, 04, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

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

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 1));

        var createResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var checkoutResult = await paymentService.CreateCheckoutSession(
            createResult.Data!.OrderId,
            model: new(),
            CancellationToken.None);

        Assert.False(checkoutResult.Succeeded);
        Assert.Contains(
            "not authorized",
            checkoutResult.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthenticatedOwner_CanCreateCheckoutSessionWithoutToken()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var utc = new DateTime(2026, 02, 05, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

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

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 1));

        var createResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var checkoutResult = await paymentService.CreateCheckoutSession(
            createResult.Data!.OrderId,
            model: new(),
            CancellationToken.None);

        Assert.True(checkoutResult.Succeeded);
    }

    [Fact]
    public async Task NonOwnerAuthenticatedUser_CannotCreateCheckoutSessionForAnotherUsersOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var utc = new DateTime(2026, 02, 06, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

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

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 1));

        var createResult = await orderService.Create(
           createServiceModel,
           CancellationToken.None);

        currentUserService.UserId = "buyer-2";
        currentUserService.Username = "buyer-2";

        var checkoutResult = await paymentService.CreateCheckoutSession(
            createResult.Data!.OrderId,
            model: new(),
            CancellationToken.None);

        Assert.False(checkoutResult.Succeeded);
        Assert.Contains(
            "not authorized",
            checkoutResult.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<BookListingDbModel> SeedApprovedListing(
        BookStackDbContext data,
        string sellerId,
        int quantity = 5)
    {
        await EnsureActiveSellerProfile(
            data,
            sellerId);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: sellerId,
            title: "Token Test Book",
            author: "Token Author");

        data.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            sellerId,
            quantity: quantity);

        data.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        return listing;
    }

    private static async Task EnsureActiveSellerProfile(
        BookStackDbContext data,
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
