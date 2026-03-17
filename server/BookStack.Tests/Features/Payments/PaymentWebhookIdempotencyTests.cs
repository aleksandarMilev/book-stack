namespace BookStack.Tests.Features.Payments;

using BookStack.Data;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Features.Orders.Shared;
using BookStack.Features.Payments.Service;
using BookStack.Features.Payments.Service.Models;
using BookStack.Features.Payments.Shared;
using BookStack.Tests.TestInfrastructure;
using BookStack.Infrastructure.Services.Result;
using BookStack.Infrastructure.Services.DateTimeProvider;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class PaymentWebhookIdempotencyTests
{
    [Fact]
    public async Task DuplicateWebhookEventId_IsIgnored_WithoutDuplicatingSideEffects()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 06, 0, 0, 0, DateTimeKind.Utc);
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
            model: new()
            {
                PaymentToken = orderCreateResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var providerPaymentId = checkoutResult.Data!.ProviderPaymentId;
        var eventId = "evt-duplicate-1";

        var payload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: eventId,
            paymentSessionId: providerPaymentId,
            status: "succeeded",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(1));

        var firstProcessResult = await paymentService.ProcessWebhook(
            provider: "mock",
            payload: payload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        var secondProcessResult = await paymentService.ProcessWebhook(
            provider: "mock",
            payload: payload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        var webhookEventsWithSameId = await data
            .PaymentWebhookEvents
            .CountAsync(
                e => e.Provider == "mock" && e.ProviderEventId == eventId,
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

        Assert.True(firstProcessResult.Succeeded);
        Assert.True(secondProcessResult.Succeeded);
        Assert.Equal(1, webhookEventsWithSameId);
        Assert.Equal(3, updatedListing.Quantity);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
    }

    [Fact]
    public async Task FailedWebhookForUnpaidOrder_ReleasesReservationAndSetsFailedStatus()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 07, 0, 0, 0, DateTimeKind.Utc);
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
            model: new()
            {
                PaymentToken = orderCreateResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var payload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-fail-release-1",
            paymentSessionId: checkoutResult.Data!.ProviderPaymentId,
            status: "failed",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(1),
            failureReason: "Insufficient funds");

        var processResult = await paymentService.ProcessWebhook(
            provider: "mock",
            payload: payload,
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

        Assert.True(processResult.Succeeded);
        Assert.Equal(5, updatedListing.Quantity);
        Assert.Equal(PaymentStatus.Failed, order.PaymentStatus);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.ReservationReleasedOnUtc);
    }

    [Fact]
    public async Task SucceededWebhook_FinalizesPayment_WithoutStockRestoration()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 08, 0, 0, 0, DateTimeKind.Utc);
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
            model: new()
            {
                PaymentToken = orderCreateResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var payload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-success-finalize-1",
            paymentSessionId: checkoutResult.Data!.ProviderPaymentId,
            status: "succeeded",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(1));

        var processResult = await paymentService.ProcessWebhook(
            provider: "mock",
            payload,
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

        Assert.True(processResult.Succeeded);
        Assert.Equal(3, updatedListing.Quantity);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
        Assert.Null(order.ReservationReleasedOnUtc);
    }

    [Fact]
    public async Task Reconciliation_WithMultipleAttempts_KeepsOrderPaidWhenAnySucceededAttemptExists()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 09, 0, 0, 0, DateTimeKind.Utc);
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
            .CreateOrderModel((listing.Id, 1));

        var orderCreateResult = await orderService.Create(
            createServiceModel,
            CancellationToken.None);

        var firstCheckout = await paymentService.CreateCheckoutSession(
            orderCreateResult.Data!.OrderId,
            model: new()
            {
                PaymentToken = orderCreateResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var secondCheckout = await paymentService.CreateCheckoutSession(
            orderCreateResult.Data.OrderId,
            model: new()
            {
                PaymentToken = orderCreateResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var firstPayload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-succeeded-1",
            paymentSessionId: firstCheckout.Data!.ProviderPaymentId,
            status: "succeeded",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(1));

        var secondPayload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-refunded-1",
            paymentSessionId: secondCheckout.Data!.ProviderPaymentId,
            status: "refunded",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(2));

        await paymentService.ProcessWebhook(
            provider: "mock",
            firstPayload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        await paymentService.ProcessWebhook(
            provider: "mock",
            secondPayload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        var order = await data
            .Orders
            .AsNoTracking()
            .SingleAsync(
                o => o.Id == orderCreateResult.Data.OrderId,
                CancellationToken.None);

        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
    }

    [Fact]
    public async Task MalformedWebhookPayload_FailsSafely_WithoutPersistingEvent()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();
        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 09, 12, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(
            data,
            dateTimeProvider,
            currentUserService);

        var result = await paymentService.ProcessWebhook(
            provider: "mock",
            payload: "{ malformed json",
            headers: new HeaderDictionary(),
            CancellationToken.None);

        var webhookEventsCount = await data
            .PaymentWebhookEvents
            .CountAsync(CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(0, webhookEventsCount);
    }

    [Fact]
    public async Task UnknownWebhookProvider_FailsSafely()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();
        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 09, 13, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(
            data,
            dateTimeProvider,
            currentUserService);

        var result = await paymentService.ProcessWebhook(
            provider: "unknown-provider",
            payload: "{}",
            headers: new HeaderDictionary(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "not supported",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SucceededWebhook_UpdatesOnlyTargetPaymentAndOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var utc = new DateTime(2026, 03, 09, 14, 0, 0, DateTimeKind.Utc);
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
            quantity: 10);

        var firstOrder = await orderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var secondOrder = await orderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var firstCheckout = await paymentService.CreateCheckoutSession(
            firstOrder.Data!.OrderId,
            model: new()
            {
                PaymentToken = firstOrder.Data.PaymentToken,
            },
            CancellationToken.None);

        var secondCheckout = await paymentService.CreateCheckoutSession(
            secondOrder.Data!.OrderId,
            model: new()
            {
                PaymentToken = secondOrder.Data.PaymentToken,
            },
            CancellationToken.None);

        var firstPayload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-target-first",
            paymentSessionId: firstCheckout.Data!.ProviderPaymentId,
            status: "succeeded",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(1));

        var result = await paymentService.ProcessWebhook(
            provider: "mock",
            payload: firstPayload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        var firstOrderDb = await data
            .Orders
            .AsNoTracking()
            .SingleAsync(o => o.Id == firstOrder.Data.OrderId);

        var secondOrderDb = await data
            .Orders
            .AsNoTracking()
            .SingleAsync(o => o.Id == secondOrder.Data.OrderId);

        var firstPayment = await data
            .Payments
            .AsNoTracking()
            .SingleAsync(p => p.ProviderPaymentId == firstCheckout.Data.ProviderPaymentId);

        var secondPayment = await data
            .Payments
            .AsNoTracking()
            .SingleAsync(p => p.ProviderPaymentId == secondCheckout.Data!.ProviderPaymentId);

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentStatus.Paid, firstOrderDb.PaymentStatus);
        Assert.Equal(OrderStatus.PendingConfirmation, firstOrderDb.Status);
        Assert.Equal(PaymentStatus.Pending, secondOrderDb.PaymentStatus);
        Assert.Equal(OrderStatus.PendingPayment, secondOrderDb.Status);
        Assert.Equal(PaymentRecordStatus.Succeeded, firstPayment.Status);
        Assert.Equal(PaymentRecordStatus.Pending, secondPayment.Status);
    }

    [Fact]
    public async Task WebhookWithInvalidSignature_FailsBeforeParsing()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();
        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 09, 15, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var provider = new StrictSignaturePaymentProvider(dateTimeProvider);
        var registry = new PaymentProviderRegistry([provider]);
        var paymentService = new PaymentService(
            data,
            dateTimeProvider,
            currentUserService,
            registry,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentService>.Instance);

        var result = await paymentService.ProcessWebhook(
            provider: provider.Name,
            payload: "{}",
            headers: new HeaderDictionary(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(provider.ParseCalled);
    }

    private static async Task<BookListingDbModel> SeedApprovedListing(
        BookStackDbContext data,
        string sellerId,
        int quantity)
    {
        await EnsureActiveSellerProfile(
            data,
            sellerId);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: sellerId,
            title: "Webhook Test Book",
            author: "Webhook Author");

        data.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            creatorId: sellerId,
            quantity);

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

    private sealed class StrictSignaturePaymentProvider(
        IDateTimeProvider dateTimeProvider) : IPaymentProvider
    {
        private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

        public bool ParseCalled { get; private set; }

        public string Name => "strict";

        public Result ValidateWebhookSignature(
            string payload,
            IHeaderDictionary headers)
            => headers.ContainsKey("X-Test-Signature")
                ? true
                : "Invalid webhook signature.";

        public Task<ResultWith<PaymentProviderCheckoutResultServiceModel>> CreateCheckoutSession(
            PaymentProviderCheckoutRequestServiceModel model,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                ResultWith<PaymentProviderCheckoutResultServiceModel>
                    .Failure("Not used in this test."));

        public ResultWith<PaymentProviderWebhookEventServiceModel> ParseWebhook(
            string payload,
            IHeaderDictionary headers)
        {
            this.ParseCalled = true;

            return ResultWith<PaymentProviderWebhookEventServiceModel>.Success(new()
            {
                ProviderEventId = "evt-test",
                ProviderPaymentId = "payment-test",
                Status = PaymentRecordStatus.Succeeded,
                OccurredOnUtc = this._dateTimeProvider.UtcNow,
            });
        }
    }
}
