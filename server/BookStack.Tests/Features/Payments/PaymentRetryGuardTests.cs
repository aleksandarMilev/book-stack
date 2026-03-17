namespace BookStack.Tests.Features.Payments;

using BookStack.Data;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Features.Orders.Data.Models;
using BookStack.Features.Orders.Shared;
using BookStack.Features.Orders.Service;
using BookStack.Features.Payments.Data.Models;
using BookStack.Features.Payments.Service;
using BookStack.Features.Payments.Shared;
using BookStack.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class PaymentRetryGuardTests
{
    [Fact]
    public async Task CreateCheckoutSession_FirstAttempt_SucceedsForPayableOnlineOrder()
    {
        await using var scope = await CreateScope();

        var listing = await SeedApprovedListing(
            scope.Data,
            sellerId: "seller-retry-1");

        var orderResult = await scope.OrderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var checkoutResult = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data!.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        Assert.True(orderResult.Succeeded);
        Assert.True(checkoutResult.Succeeded);
        Assert.NotNull(checkoutResult.Data);
    }

    [Fact]
    public async Task CreateCheckoutSession_SecondConcurrentPendingAttempt_IsBlocked()
    {
        await using var scope = await CreateScope();

        var listing = await SeedApprovedListing(
            scope.Data,
            sellerId: "seller-retry-2");

        var orderResult = await scope.OrderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var firstCheckout = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data!.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var secondCheckout = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        Assert.True(firstCheckout.Succeeded);
        Assert.False(secondCheckout.Succeeded);
        Assert.Contains(
            "active pending payment attempt",
            secondCheckout.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCheckoutSession_RetryAfterFailedAttempt_IsAllowedWhenOrderStillPayable()
    {
        await using var scope = await CreateScope();

        var listing = await SeedApprovedListing(
            scope.Data,
            sellerId: "seller-retry-3");

        var orderResult = await scope.OrderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var firstCheckout = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data!.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var payment = await scope.Data
            .Payments
            .SingleAsync(p => p.Id == firstCheckout.Data!.PaymentId);

        payment.Status = PaymentRecordStatus.Failed;

        var order = await scope.Data
            .Orders
            .SingleAsync(o => o.Id == orderResult.Data.OrderId);

        order.PaymentStatus = PaymentStatus.Failed;
        order.Status = OrderStatus.PendingPayment;
        order.ReservationReleasedOnUtc = null;
        order.ReservationExpiresOnUtc = scope.DateTimeProvider.UtcNow.AddMinutes(10);

        await scope.Data.SaveChangesAsync(CancellationToken.None);

        var retryCheckout = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        Assert.True(retryCheckout.Succeeded);
    }

    [Fact]
    public async Task CreateCheckoutSession_RetryAfterCanceledAttempt_IsAllowedWhenOrderStillPayable()
    {
        await using var scope = await CreateScope();

        var listing = await SeedApprovedListing(
            scope.Data,
            sellerId: "seller-retry-4");

        var orderResult = await scope.OrderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var firstCheckout = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data!.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var payment = await scope.Data
            .Payments
            .SingleAsync(p => p.Id == firstCheckout.Data!.PaymentId);

        payment.Status = PaymentRecordStatus.Canceled;

        var order = await scope.Data
            .Orders
            .SingleAsync(o => o.Id == orderResult.Data.OrderId);

        order.PaymentStatus = PaymentStatus.Cancelled;
        order.Status = OrderStatus.PendingPayment;
        order.ReservationReleasedOnUtc = null;
        order.ReservationExpiresOnUtc = scope.DateTimeProvider.UtcNow.AddMinutes(10);

        await scope.Data.SaveChangesAsync(CancellationToken.None);

        var retryCheckout = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        Assert.True(retryCheckout.Succeeded);
    }

    [Fact]
    public async Task CreateCheckoutSession_RetryAfterExpiredPaymentStatus_IsAllowedOnlyWhenOrderIsStillPayable()
    {
        await using var scope = await CreateScope();

        var listing = await SeedApprovedListing(
            scope.Data,
            sellerId: "seller-retry-5");

        var orderResult = await scope.OrderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var firstCheckout = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data!.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var payment = await scope.Data
            .Payments
            .SingleAsync(p => p.Id == firstCheckout.Data!.PaymentId);

        payment.Status = PaymentRecordStatus.Failed;

        var order = await scope.Data
            .Orders
            .SingleAsync(o => o.Id == orderResult.Data.OrderId);

        order.PaymentStatus = PaymentStatus.Expired;
        order.Status = OrderStatus.PendingPayment;
        order.ReservationReleasedOnUtc = null;
        order.ReservationExpiresOnUtc = scope.DateTimeProvider.UtcNow.AddMinutes(10);

        await scope.Data.SaveChangesAsync(CancellationToken.None);

        var retryWhilePayable = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        Assert.True(retryWhilePayable.Succeeded);

        order.Status = OrderStatus.Expired;
        await scope.Data.SaveChangesAsync(CancellationToken.None);

        var retryWhenNotPayable = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        Assert.False(retryWhenNotPayable.Succeeded);
    }

    [Fact]
    public async Task CreateCheckoutSession_RetryIsBlockedAfterSuccessfulPayment()
    {
        await using var scope = await CreateScope();

        var listing = await SeedApprovedListing(
            scope.Data,
            sellerId: "seller-retry-6");

        var orderResult = await scope.OrderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var firstCheckout = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data!.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var successPayload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-retry-success",
            paymentSessionId: firstCheckout.Data!.ProviderPaymentId,
            status: "succeeded",
            occurredOnUtc: scope.DateTimeProvider.UtcNow.AddMinutes(1));

        var webhookResult = await scope.PaymentService.ProcessWebhook(
            provider: "mock",
            payload: successPayload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        var retryCheckout = await scope.PaymentService.CreateCheckoutSession(
            orderResult.Data.OrderId,
            new()
            {
                PaymentToken = orderResult.Data.PaymentToken,
            },
            CancellationToken.None);

        Assert.True(webhookResult.Succeeded);
        Assert.False(retryCheckout.Succeeded);
        Assert.Contains(
            "finalized",
            retryCheckout.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCheckoutSession_RetryIsBlockedForCancelledOrder()
    {
        await using var scope = await CreateScope();

        var listing = await SeedApprovedListing(
            scope.Data,
            sellerId: "seller-retry-7");

        var orderResult = await scope.OrderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        Assert.True(orderResult.Succeeded);
        var createdOrder = orderResult.Data;
        Assert.NotNull(createdOrder);

        var order = await scope.Data
            .Orders
            .SingleAsync(o => o.Id == createdOrder!.OrderId);

        order.Status = OrderStatus.Cancelled;
        order.ReservationReleasedOnUtc = scope.DateTimeProvider.UtcNow;

        await scope.Data.SaveChangesAsync(CancellationToken.None);

        var retryCheckout = await scope.PaymentService.CreateCheckoutSession(
            createdOrder.OrderId,
            new()
            {
                PaymentToken = createdOrder.PaymentToken,
            },
            CancellationToken.None);

        Assert.False(retryCheckout.Succeeded);
    }

    private static async Task<BookListingDbModel> SeedApprovedListing(
        BookStackDbContext data,
        string sellerId)
    {
        await EnsureActiveSellerProfile(
            data,
            sellerId);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: sellerId,
            title: "Retry Guard Book",
            author: "Retry Guard Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            creatorId: sellerId,
            quantity: 10);

        data.BookListings.Add(listing);
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

    private static async Task<TestScope> CreateScope()
    {
        var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();
        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 12, 0, 0, DateTimeKind.Utc));

        var data = database.CreateDbContext(
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

        return new TestScope(
            database,
            data,
            dateTimeProvider,
            paymentService,
            orderService);
    }

    private sealed class TestScope(
        TestDatabaseScope database,
        BookStackDbContext data,
        TestDateTimeProvider dateTimeProvider,
        PaymentService paymentService,
        OrderService orderService) : IAsyncDisposable
    {
        public TestDatabaseScope Database { get; } = database;

        public BookStackDbContext Data { get; } = data;

        public TestDateTimeProvider DateTimeProvider { get; } = dateTimeProvider;

        public PaymentService PaymentService { get; } = paymentService;

        public OrderService OrderService { get; } = orderService;

        public async ValueTask DisposeAsync()
        {
            await this.Data.DisposeAsync();
            await this.Database.DisposeAsync();
        }
    }
}
