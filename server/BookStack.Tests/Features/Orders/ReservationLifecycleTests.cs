namespace BookStack.Tests.Features.Orders;

using BookStack.Data;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Features.Orders.Shared;
using BookStack.Features.Payments.Service.Models;
using BookStack.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class ReservationLifecycleTests
{
    [Fact]
    public async Task OrderCreation_ReducesListingQuantityAsReservedStock()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 01, 0, 0, 0, DateTimeKind.Utc);
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
            sellerId:
            "seller-1",
            quantity: 5);

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 2));

        var createResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var updatedListing = await data
            .BookListings
            .AsNoTracking()
            .SingleAsync(
                l => l.Id == listing.Id,
                CancellationToken.None);

        Assert.True(createResult.Succeeded);
        Assert.Equal(3, updatedListing.Quantity);
    }

    [Fact]
    public async Task ReservationRelease_RestoresStockOnlyOnce_WhenCalledMultipleTimes()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 02, 0, 0, 0, DateTimeKind.Utc);
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
            sellerId: "seller-1",
            quantity: 5);

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 2));

        var createResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var orderId = createResult.Data!.OrderId;

        var firstReleaseResult = await paymentService.ReleaseOrderReservation(
            orderId,
            CancellationToken.None);

        var secondReleaseResult = await paymentService.ReleaseOrderReservation(
            orderId,
            CancellationToken.None);

        var updatedListing = await data
            .BookListings
            .AsNoTracking()
            .SingleAsync(
                l => l.Id == listing.Id,
                CancellationToken.None);

        var order = await data
            .Orders
            .AsNoTracking()
            .SingleAsync(
                o => o.Id == orderId,
                CancellationToken.None);

        Assert.True(firstReleaseResult.Succeeded);
        Assert.True(secondReleaseResult.Succeeded);
        Assert.Equal(5, updatedListing.Quantity);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.ReservationReleasedOnUtc);
    }

    [Fact]
    public async Task ExpiredReservations_RestoreStockOnce_AndMoveOrderToExpired()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 03, 0, 0, 0, DateTimeKind.Utc);
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
            sellerId: "seller-1",
            quantity: 5);

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 2));

        var createResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var orderId = createResult.Data!.OrderId;
        var order = await data
            .Orders
            .SingleAsync(
                o => o.Id == orderId,
                CancellationToken.None);

        dateTimeProvider.UtcNow = order
            .ReservationExpiresOnUtc
            .AddMinutes(1);

        await paymentService
            .ReleaseExpiredReservations(CancellationToken.None);

        await paymentService
            .ReleaseExpiredReservations(CancellationToken.None);

        var updatedListing = await data
            .BookListings
            .AsNoTracking()
            .SingleAsync(
                l => l.Id == listing.Id,
                CancellationToken.None);

        await data
            .Entry(order)
            .ReloadAsync();

        Assert.Equal(5, updatedListing.Quantity);
        Assert.Equal(OrderStatus.Expired, order.Status);
        Assert.NotNull(order.ReservationReleasedOnUtc);
    }

    [Fact]
    public async Task SuccessfulPayment_KeepsReservedStockReduced()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 04, 0, 0, 0, DateTimeKind.Utc);
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
            sellerId: "seller-1",
            quantity: 5);

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 2));

        var orderCreateResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var checkoutResult = await paymentService.CreateCheckoutSession(
            orderCreateResult.Data!.OrderId,
            new CreatePaymentSessionServiceModel
            {
                PaymentToken = orderCreateResult.Data.PaymentToken,
            });

        var webhookPayload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-success-1",
            paymentSessionId: checkoutResult.Data!.ProviderPaymentId,
            status: "succeeded",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(1));

        var webhookResult = await paymentService.ProcessWebhook(
            provider: "mock",
            payload: webhookPayload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        var updatedListing = await data
            .BookListings
            .AsNoTracking()
            .SingleAsync(
                l => l.Id == listing.Id,
                CancellationToken.None);

        var order = await data
            .Orders
            .AsNoTracking()
            .SingleAsync(
                o => o.Id == orderCreateResult.Data.OrderId,
                CancellationToken.None);

        Assert.True(webhookResult.Succeeded);
        Assert.Equal(3, updatedListing.Quantity);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Null(order.ReservationReleasedOnUtc);
    }

    [Fact]
    public async Task FailedPayment_ReleasesReservationAndRestoresStock()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 05, 0, 0, 0, DateTimeKind.Utc);
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
            sellerId: "seller-1",
            quantity: 5);

        var createServiceModel = MarketplaceTestData
            .CreateOrderModel((listing.Id, 2));

        var orderCreateResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var checkoutResult = await paymentService.CreateCheckoutSession(
            orderCreateResult.Data!.OrderId,
            new CreatePaymentSessionServiceModel
            {
                PaymentToken = orderCreateResult.Data.PaymentToken,
            });

        var webhookPayload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-failed-1",
            paymentSessionId: checkoutResult.Data!.ProviderPaymentId,
            status: "failed",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(1),
            failureReason: "Card declined");

        var webhookResult = await paymentService.ProcessWebhook(
            provider: "mock",
            payload: webhookPayload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        var updatedListing = await data
            .BookListings
            .AsNoTracking()
            .SingleAsync(
                l => l.Id == listing.Id,
                CancellationToken.None);

        var order = await data
            .Orders
            .AsNoTracking()
            .SingleAsync(
                o => o.Id == orderCreateResult.Data.OrderId,
                CancellationToken.None);

        Assert.True(webhookResult.Succeeded);
        Assert.Equal(5, updatedListing.Quantity);
        Assert.Equal(PaymentStatus.Failed, order.PaymentStatus);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.ReservationReleasedOnUtc);
    }

    private static async Task<BookListingDbModel> SeedApprovedListing(
        BookStackDbContext data,
        string sellerId,
        int quantity)
    {
        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: sellerId,
            title: "Reservation Test Book",
            author: "Reservation Author");

        data.Add(book);
        await data.SaveChangesAsync();

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            creatorId: sellerId,
            quantity);

        data.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        return listing;
    }
}
