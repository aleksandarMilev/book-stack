namespace BookStack.Tests.Features.Orders;

using BookStack.Data;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Features.Orders.Shared;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

public class OrderBusinessModelAlignmentTests
{
    [Fact]
    public async Task Create_FailsWhenOrderContainsListingsFromMultipleSellers()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 15, 0, 0, 0, DateTimeKind.Utc));

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

        var sellerAListing = await SeedApprovedListing(
            data,
            sellerId: "seller-a",
            title: "Seller A");

        var sellerBListing = await SeedApprovedListing(
            data,
            sellerId: "seller-b",
            title: "Seller B");

        var result = await orderService.Create(
            MarketplaceTestData.CreateOrderModel(
                (sellerAListing.Id, 1),
                (sellerBListing.Id, 1)),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "same seller",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_FailsWhenSelectedPaymentMethodIsNotSupportedBySeller()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 15, 1, 0, 0, DateTimeKind.Utc));

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
            sellerId: "seller-online-only",
            title: "Online Only Seller",
            supportsOnlinePayment: true,
            supportsCashOnDelivery: false);

        var result = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "not supported",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_OnlinePaymentOrder_SetsInitialStatusesAndSettlement()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 15, 2, 0, 0, DateTimeKind.Utc));

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
            sellerId: "seller-online",
            title: "Online Order Book");

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.Online,
                (listing.Id, 1)),
            CancellationToken.None);

        var order = await data
            .Orders
            .AsNoTracking()
            .SingleAsync(
                o => o.Id == createResult.Data!.OrderId,
                CancellationToken.None);

        Assert.True(createResult.Succeeded);
        Assert.Equal(OrderPaymentMethod.Online, order.PaymentMethod);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(SettlementStatus.Pending, order.SettlementStatus);
    }

    [Fact]
    public async Task Create_CashOnDeliveryOrder_SetsInitialStatusesAndSettlement()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 15, 3, 0, 0, DateTimeKind.Utc));

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
            sellerId: "seller-cod",
            title: "COD Order Book",
            supportsCashOnDelivery: true);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.CashOnDelivery,
                (listing.Id, 1)),
            CancellationToken.None);

        var order = await data
            .Orders
            .AsNoTracking()
            .SingleAsync(
                o => o.Id == createResult.Data!.OrderId,
                CancellationToken.None);

        Assert.True(createResult.Succeeded);
        Assert.Equal(OrderPaymentMethod.CashOnDelivery, order.PaymentMethod);
        Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
        Assert.Equal(PaymentStatus.NotRequired, order.PaymentStatus);
        Assert.Equal(SettlementStatus.Pending, order.SettlementStatus);
        Assert.Null(createResult.Data!.PaymentToken);
    }

    [Fact]
    public async Task Create_StoresCommissionSnapshotUsingConfiguredFeeWithRounding()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 15, 4, 0, 0, DateTimeKind.Utc));

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
            sellerId: "seller-fee",
            title: "Fee Snapshot Book",
            price: 19.99m);

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.Online,
                (listing.Id, 3)),
            CancellationToken.None);

        var order = await data
            .Orders
            .AsNoTracking()
            .SingleAsync(
                o => o.Id == createResult.Data!.OrderId,
                CancellationToken.None);

        Assert.True(createResult.Succeeded);
        Assert.Equal(59.97m, order.TotalAmount);
        Assert.Equal(10m, order.PlatformFeePercent);
        Assert.Equal(6.00m, order.PlatformFeeAmount);
        Assert.Equal(53.97m, order.SellerNetAmount);
    }

    [Fact]
    public async Task Create_FailsWhenListingCurrencyIsNotEur()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService();

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 15, 5, 0, 0, DateTimeKind.Utc));

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
            sellerId: "seller-non-eur",
            title: "Non-EUR Order Book",
            currency: "USD");

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModelWithPaymentMethod(
                OrderPaymentMethod.Online,
                (listing.Id, 1)),
            CancellationToken.None);

        Assert.False(createResult.Succeeded);
        Assert.Contains(
            "EUR",
            createResult.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<BookListingDbModel> SeedApprovedListing(
        BookStackDbContext data,
        string sellerId,
        string title,
        decimal price = 20m,
        string currency = "EUR",
        bool supportsOnlinePayment = true,
        bool supportsCashOnDelivery = true)
    {
        await EnsureSellerProfile(
            data,
            sellerId,
            supportsOnlinePayment,
            supportsCashOnDelivery);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: sellerId,
            title,
            author: "Business Model Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            bookId: book.Id,
            creatorId: sellerId,
            price: price,
            currency: currency,
            quantity: 20);

        data.BookListings.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        return listing;
    }

    private static async Task EnsureSellerProfile(
        BookStackDbContext data,
        string sellerId,
        bool supportsOnlinePayment,
        bool supportsCashOnDelivery)
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

        var sellerProfile = await data
            .SellerProfiles
            .SingleOrDefaultAsync(
                p => p.UserId == sellerId,
                CancellationToken.None);

        if (sellerProfile is null)
        {
            sellerProfile = MarketplaceTestData.CreateSellerProfile(
                sellerId,
                isActive: true,
                supportsOnlinePayment: supportsOnlinePayment,
                supportsCashOnDelivery: supportsCashOnDelivery);

            data.SellerProfiles.Add(sellerProfile);
        }
        else
        {
            sellerProfile.IsActive = true;
            sellerProfile.SupportsOnlinePayment = supportsOnlinePayment;
            sellerProfile.SupportsCashOnDelivery = supportsCashOnDelivery;
        }

        await data.SaveChangesAsync(CancellationToken.None);
    }
}
