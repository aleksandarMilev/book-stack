namespace BookStack.Tests.Features.Orders;

using BookStack.Features.Orders.Service.Models;
using BookStack.Tests.TestInfrastructure;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Data;
using Microsoft.EntityFrameworkCore;

public class SellerOrderVisibilityTests
{
    [Fact]
    public async Task SellerViews_ShowOnlyOrdersContainingSellersItems_AndDetailsContainOnlySellerItems()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var utc = new DateTime(2026, 03, 10, 0, 0, 0, DateTimeKind.Utc);
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

        var sellerAListing = await SeedApprovedListing(
            data,
            sellerId: "seller-a",
            title: "Seller A Book");

        var sellerBListing = await SeedApprovedListing(
            data,
            sellerId: "seller-b",
            title: "Seller B Book");

        var sellerAOrderCreateServiceModel = MarketplaceTestData.CreateOrderModel(
            (sellerAListing.Id, 1));

        var sellerAOrderResult = await orderService.Create(
           sellerAOrderCreateServiceModel,
           CancellationToken.None);

        var sellerBOnlyCreateServiceModel = MarketplaceTestData
            .CreateOrderModel((sellerBListing.Id, 1));

        var sellerBOnlyOrderResult = await orderService.Create(
            sellerBOnlyCreateServiceModel,
            CancellationToken.None);

        currentUserService.UserId = "seller-a";
        currentUserService.Username = "seller-a";
        currentUserService.Admin = false;

        var filterServiceModel = new OrderFilterServiceModel();
        var soldOrders = await orderService.Sold(
            filterServiceModel,
            CancellationToken.None);

        var soldOrderDetails = await orderService.SoldDetails(
            sellerAOrderResult.Data!.OrderId,
            CancellationToken.None);

        var forbiddenOrderDetails = await orderService.SoldDetails(
            sellerBOnlyOrderResult.Data!.OrderId,
            CancellationToken.None);

        Assert.Equal(1, soldOrders.TotalItems);
        Assert.Single(soldOrders.Items);
        Assert.Equal(
            sellerAOrderResult.Data.OrderId,
            soldOrders.Items.Single().Id);

        Assert.NotNull(soldOrderDetails);
        Assert.Single(soldOrderDetails!.Items);
        Assert.Equal(
            sellerAListing.Id,
            soldOrderDetails.Items.Single().ListingId);

        Assert.Null(forbiddenOrderDetails);
    }

    [Fact]
    public async Task BuyerAndAdminViews_AreUnaffectedBySellerFiltering()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var utc = new DateTime(2026, 03, 11, 0, 0, 0, DateTimeKind.Utc);
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

        var sellerAListing = await SeedApprovedListing(
            data,
            sellerId: "seller-a",
            title: "Seller A Book");

        var sellerBListing = await SeedApprovedListing(
            data,
            sellerId: "seller-b",
            title: "Seller B Book");

        var sellerAOrderCreateServiceModel = MarketplaceTestData.CreateOrderModel(
            (sellerAListing.Id, 1));

        var sellerAOrderResult = await orderService.Create(
            sellerAOrderCreateServiceModel,
            CancellationToken.None);

        var sellerBOnlyCreateServiceModel = MarketplaceTestData
            .CreateOrderModel((sellerBListing.Id, 1));

        var sellerBOnlyOrderResult = await orderService.Create(
            sellerBOnlyCreateServiceModel,
            CancellationToken.None);

        var buyerOrdersFilterServiceModel = new OrderFilterServiceModel();
        var buyerOrders = await orderService.Mine(
            buyerOrdersFilterServiceModel,
            CancellationToken.None);

        var buyerOrderDetails = await orderService.Details(
            sellerAOrderResult.Data!.OrderId,
            CancellationToken.None);

        currentUserService.UserId = "admin-1";
        currentUserService.Username = "admin-1";
        currentUserService.Admin = true;

        var adminOrdersFilterServiceModel = new OrderFilterServiceModel();
        var adminOrders = await orderService.All(
            adminOrdersFilterServiceModel,
            CancellationToken.None);

        var adminOrderDetails = await orderService.Details(
            sellerAOrderResult.Data.OrderId,
            CancellationToken.None);

        Assert.Equal(2, buyerOrders.TotalItems);
        Assert.NotNull(buyerOrderDetails);
        Assert.Single(buyerOrderDetails!.Items);
        Assert.Contains(
            buyerOrders.Items,
            o => o.Id == sellerBOnlyOrderResult.Data!.OrderId);

        Assert.Equal(2, adminOrders.TotalItems);
        Assert.NotNull(adminOrderDetails);
        Assert.Single(adminOrderDetails!.Items);
    }

    [Fact]
    public async Task Buyer_CannotAccessAnotherBuyersOrderDetails()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var utc = new DateTime(2026, 03, 12, 0, 0, 0, DateTimeKind.Utc);
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
            sellerId: "seller-a",
            title: "Protected Buyer Order");

        var orderResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "buyer-2";
        currentUserService.Username = "buyer-2";

        var details = await orderService.Details(
            orderResult.Data!.OrderId,
            CancellationToken.None);

        Assert.Null(details);
    }

    private static async Task<BookListingDbModel> SeedApprovedListing(
        BookStackDbContext data,
        string sellerId,
        string title)
    {
        await EnsureActiveSellerProfile(
            data,
            sellerId);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: sellerId,
            title,
            author: "Seller Author");

        data.Add(book);
        await data.SaveChangesAsync();

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            creatorId: sellerId,
            quantity: 10);

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
