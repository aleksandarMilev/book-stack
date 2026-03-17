namespace BookStack.Tests.Features.Orders;

using BookStack.Data;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Features.Orders.Shared;
using BookStack.Features.Payments.Service.Models;
using BookStack.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class OrderLifecycleStateMachineTests
{
    [Fact]
    public async Task AdminChangeStatus_RejectsInvalidJump_FromPendingPaymentToShipped()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 0, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(data, sellerId: "seller-1");
        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.Online,
                (listing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "admin-1";
        currentUserService.Username = "admin-1";
        currentUserService.Admin = true;

        var result = await orderService.ChangeStatus(
            createResult.Data!.OrderId,
            OrderStatus.Shipped,
            CancellationToken.None);

        var order = await data.Orders.AsNoTracking().SingleAsync(o => o.Id == createResult.Data.OrderId);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public async Task AdminChangeStatus_RejectsResurrectionOfCompletedOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 1, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1",
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        var order = await data.Orders.SingleAsync(o => o.Id == createResult.Data!.OrderId);
        order.Status = OrderStatus.Completed;
        await data.SaveChangesAsync(CancellationToken.None);

        currentUserService.UserId = "admin-1";
        currentUserService.Username = "admin-1";
        currentUserService.Admin = true;

        var result = await orderService.ChangeStatus(
            createResult.Data!.OrderId,
            OrderStatus.PendingConfirmation,
            CancellationToken.None);

        await data.Entry(order).ReloadAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public async Task SellerFulfillment_CanProgressOwnCodOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 2, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1",
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "seller-1";
        currentUserService.Username = "seller-1";
        currentUserService.Admin = false;

        var confirmResult = await orderService.ConfirmSoldOrder(createResult.Data!.OrderId, CancellationToken.None);
        var shipResult = await orderService.ShipSoldOrder(createResult.Data.OrderId, CancellationToken.None);
        var deliverResult = await orderService.DeliverSoldOrder(createResult.Data.OrderId, CancellationToken.None);

        var order = await data.Orders.AsNoTracking().SingleAsync(o => o.Id == createResult.Data.OrderId);

        Assert.True(confirmResult.Succeeded);
        Assert.True(shipResult.Succeeded);
        Assert.True(deliverResult.Succeeded);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public async Task RepeatedSellerConfirm_IsSafeNoOp()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 2, 30, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1",
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "seller-1";
        currentUserService.Username = "seller-1";

        var firstConfirm = await orderService.ConfirmSoldOrder(
            createResult.Data!.OrderId,
            CancellationToken.None);

        var secondConfirm = await orderService.ConfirmSoldOrder(
            createResult.Data.OrderId,
            CancellationToken.None);

        var order = await data.Orders.AsNoTracking().SingleAsync(o => o.Id == createResult.Data.OrderId);

        Assert.True(firstConfirm.Succeeded);
        Assert.True(secondConfirm.Succeeded);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task SellerFulfillment_CannotChangeUnrelatedOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 3, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1",
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        await EnsureActiveSellerProfile(data, "seller-2");

        currentUserService.UserId = "seller-2";
        currentUserService.Username = "seller-2";
        currentUserService.Admin = false;

        var result = await orderService.ConfirmSoldOrder(
            createResult.Data!.OrderId,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("can not modify", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SellerFulfillment_CannotPerformInvalidTransition()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 4, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1",
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "seller-1";
        currentUserService.Username = "seller-1";
        currentUserService.Admin = false;

        var shipResult = await orderService.ShipSoldOrder(
            createResult.Data!.OrderId,
            CancellationToken.None);

        Assert.False(shipResult.Succeeded);
        Assert.Contains("not allowed", shipResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InactiveSeller_CannotUseSellerFulfillmentActions()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 4, 30, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1",
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        var profile = await data
            .SellerProfiles
            .SingleAsync(p => p.UserId == "seller-1");

        profile.IsActive = false;
        await data.SaveChangesAsync(CancellationToken.None);

        currentUserService.UserId = "seller-1";
        currentUserService.Username = "seller-1";
        currentUserService.Admin = false;

        var result = await orderService.ConfirmSoldOrder(
            createResult.Data!.OrderId,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "active seller profile",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SellerFulfillment_CannotConfirmOnlineOrderWhenPaymentIsNotPaid()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 5, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(data, sellerId: "seller-1");
        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.Online,
                (listing.Id, 1)),
            CancellationToken.None);

        var order = await data.Orders.SingleAsync(o => o.Id == createResult.Data!.OrderId);
        order.Status = OrderStatus.PendingConfirmation;
        order.PaymentStatus = PaymentStatus.Pending;
        await data.SaveChangesAsync(CancellationToken.None);

        currentUserService.UserId = "seller-1";
        currentUserService.Username = "seller-1";
        currentUserService.Admin = false;

        var result = await orderService.ConfirmSoldOrder(
            createResult.Data!.OrderId,
            CancellationToken.None);

        await data.Entry(order).ReloadAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
    }

    [Fact]
    public async Task FailedPaymentOrder_CannotBeFulfilledBySeller()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 6, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(data, sellerId: "seller-1");
        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.Online,
                (listing.Id, 1)),
            CancellationToken.None);

        var checkoutResult = await paymentService.CreateCheckoutSession(
            createResult.Data!.OrderId,
            new CreatePaymentSessionServiceModel
            {
                PaymentToken = createResult.Data.PaymentToken,
            },
            CancellationToken.None);

        var payload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-fail-fulfillment",
            paymentSessionId: checkoutResult.Data!.ProviderPaymentId,
            status: "failed",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(1),
            failureReason: "Declined");

        await paymentService.ProcessWebhook(
            provider: "mock",
            payload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        currentUserService.UserId = "seller-1";
        currentUserService.Username = "seller-1";
        currentUserService.Admin = false;

        var confirmResult = await orderService.ConfirmSoldOrder(
            createResult.Data.OrderId,
            CancellationToken.None);

        var order = await data.Orders.AsNoTracking().SingleAsync(o => o.Id == createResult.Data.OrderId);

        Assert.False(confirmResult.Succeeded);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task CodOrders_AreUnaffectedByPaymentWebhookProcessing()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 7, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1",
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        var webhookPayload = MarketplaceTestData.CreateMockWebhookPayload(
            eventId: "evt-cod-ignored",
            paymentSessionId: "missing-cod-payment",
            status: "succeeded",
            occurredOnUtc: dateTimeProvider.UtcNow.AddMinutes(1));

        var webhookResult = await paymentService.ProcessWebhook(
            provider: "mock",
            payload: webhookPayload,
            headers: new HeaderDictionary(),
            CancellationToken.None);

        var order = await data.Orders.AsNoTracking().SingleAsync(o => o.Id == createResult.Data!.OrderId);

        Assert.True(webhookResult.Succeeded);
        Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
        Assert.Equal(PaymentStatus.NotRequired, order.PaymentStatus);
    }

    [Fact]
    public async Task Settlement_CannotBeSettledForUnpaidOnlineOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 8, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(data, sellerId: "seller-1");
        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.Online,
                (listing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "admin-1";
        currentUserService.Username = "admin-1";
        currentUserService.Admin = true;

        var settleResult = await orderService.ChangeSettlementStatus(
            createResult.Data!.OrderId,
            SettlementStatus.Settled,
            CancellationToken.None);

        var order = await data.Orders.AsNoTracking().SingleAsync(o => o.Id == createResult.Data.OrderId);

        Assert.False(settleResult.Succeeded);
        Assert.Equal(SettlementStatus.Pending, order.SettlementStatus);
    }

    [Fact]
    public async Task Settlement_OnlinePaidOrderCanBeSettled()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 9, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(data, sellerId: "seller-1");
        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.Online,
                (listing.Id, 1)),
            CancellationToken.None);

        var order = await data.Orders.SingleAsync(o => o.Id == createResult.Data!.OrderId);
        order.PaymentStatus = PaymentStatus.Paid;
        order.Status = OrderStatus.PendingConfirmation;
        await data.SaveChangesAsync(CancellationToken.None);

        currentUserService.UserId = "admin-1";
        currentUserService.Username = "admin-1";
        currentUserService.Admin = true;

        var settleResult = await orderService.ChangeSettlementStatus(
            createResult.Data!.OrderId,
            SettlementStatus.Settled,
            CancellationToken.None);

        await data.Entry(order).ReloadAsync();

        Assert.True(settleResult.Succeeded);
        Assert.Equal(SettlementStatus.Settled, order.SettlementStatus);
    }

    [Fact]
    public async Task Settlement_CodOrderBecomesEligibleWhenDelivered()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 10, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1",
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "admin-1";
        currentUserService.Username = "admin-1";
        currentUserService.Admin = true;

        var earlySettlement = await orderService.ChangeSettlementStatus(
            createResult.Data!.OrderId,
            SettlementStatus.Settled,
            CancellationToken.None);

        var order = await data.Orders.SingleAsync(o => o.Id == createResult.Data.OrderId);
        order.Status = OrderStatus.Delivered;
        await data.SaveChangesAsync(CancellationToken.None);

        var deliveredSettlement = await orderService.ChangeSettlementStatus(
            createResult.Data.OrderId,
            SettlementStatus.Settled,
            CancellationToken.None);

        await data.Entry(order).ReloadAsync();

        Assert.False(earlySettlement.Succeeded);
        Assert.True(deliveredSettlement.Succeeded);
        Assert.Equal(SettlementStatus.Settled, order.SettlementStatus);
    }

    [Fact]
    public async Task AdminExpiry_ReleasesReservationAndRestoresStock()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 11, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(data, sellerId: "seller-1", quantity: 5);
        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.Online,
                (listing.Id, 2)),
            CancellationToken.None);

        currentUserService.UserId = "admin-1";
        currentUserService.Username = "admin-1";
        currentUserService.Admin = true;

        var expireResult = await orderService.ChangeStatus(
            createResult.Data!.OrderId,
            OrderStatus.Expired,
            CancellationToken.None);

        var order = await data.Orders.AsNoTracking().SingleAsync(o => o.Id == createResult.Data.OrderId);
        var updatedListing = await data.BookListings.AsNoTracking().SingleAsync(l => l.Id == listing.Id);

        Assert.True(expireResult.Succeeded);
        Assert.Equal(OrderStatus.Expired, order.Status);
        Assert.NotNull(order.ReservationReleasedOnUtc);
        Assert.Equal(5, updatedListing.Quantity);
    }

    [Fact]
    public async Task CompletingFulfillment_DoesNotReleaseReservedInventory()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 16, 12, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(currentUserService, dateTimeProvider);

        var paymentService = TestServiceFactory.CreatePaymentService(data, dateTimeProvider, currentUserService);
        var orderService = TestServiceFactory.CreateOrderService(data, currentUserService, paymentService, dateTimeProvider);

        var listing = await SeedApprovedListing(
            data,
            sellerId: "seller-1",
            quantity: 5,
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 2)),
            CancellationToken.None);

        currentUserService.UserId = "seller-1";
        currentUserService.Username = "seller-1";
        currentUserService.Admin = false;

        await orderService.ConfirmSoldOrder(createResult.Data!.OrderId, CancellationToken.None);
        await orderService.ShipSoldOrder(createResult.Data.OrderId, CancellationToken.None);
        await orderService.DeliverSoldOrder(createResult.Data.OrderId, CancellationToken.None);

        currentUserService.UserId = "admin-1";
        currentUserService.Username = "admin-1";
        currentUserService.Admin = true;

        var completeResult = await orderService.ChangeStatus(
            createResult.Data.OrderId,
            OrderStatus.Completed,
            CancellationToken.None);

        var order = await data.Orders.AsNoTracking().SingleAsync(o => o.Id == createResult.Data.OrderId);
        var updatedListing = await data.BookListings.AsNoTracking().SingleAsync(l => l.Id == listing.Id);

        Assert.True(completeResult.Succeeded);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(3, updatedListing.Quantity);
        Assert.Null(order.ReservationReleasedOnUtc);
    }

    private static async Task<BookListingDbModel> SeedApprovedListing(
        BookStackDbContext data,
        string sellerId,
        int quantity = 10,
        bool supportsOnlinePayment = true,
        bool supportsCashOnDelivery = true)
    {
        await EnsureActiveSellerProfile(
            data,
            sellerId,
            supportsOnlinePayment,
            supportsCashOnDelivery);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: sellerId,
            title: $"Lifecycle Book {Guid.NewGuid():N}",
            author: "Lifecycle Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            creatorId: sellerId,
            quantity: quantity);

        data.BookListings.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        return listing;
    }

    private static async Task EnsureActiveSellerProfile(
        BookStackDbContext data,
        string sellerId,
        bool supportsOnlinePayment = true,
        bool supportsCashOnDelivery = true)
    {
        var userExists = await data.Users.AnyAsync(u => u.Id == sellerId);
        if (!userExists)
        {
            data.Users.Add(MarketplaceTestData.CreateUser(
                sellerId,
                $"{sellerId}@example.com"));
        }

        var profile = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(p => p.UserId == sellerId);

        if (profile is null)
        {
            profile = MarketplaceTestData.CreateSellerProfile(
                sellerId,
                isActive: true,
                supportsOnlinePayment: supportsOnlinePayment,
                supportsCashOnDelivery: supportsCashOnDelivery);

            data.SellerProfiles.Add(profile);
        }
        else
        {
            profile.IsActive = true;
            profile.SupportsOnlinePayment = supportsOnlinePayment;
            profile.SupportsCashOnDelivery = supportsCashOnDelivery;
        }

        await data.SaveChangesAsync(CancellationToken.None);
    }
}
