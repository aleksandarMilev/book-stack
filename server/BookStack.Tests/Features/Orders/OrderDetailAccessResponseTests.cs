namespace BookStack.Tests.Features.Orders;

using BookStack.Data;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Features.Orders.Service.Models;
using BookStack.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminOrdersController = BookStack.Features.Orders.Web.Admin.OrdersController;
using UserOrdersController = BookStack.Features.Orders.Web.User.OrdersController;

public class OrderDetailAccessResponseTests
{
    [Fact]
    public async Task BuyerDetails_ReturnsOk_ForOwnOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 12, 0, 0, DateTimeKind.Utc));

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
            title: "Buyer Owned Order");

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        var controller = new UserOrdersController(orderService);
        var response = await controller.Details(
            createResult.Data!.OrderId,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var model = Assert.IsType<OrderServiceModel>(okResult.Value);
        Assert.Equal(createResult.Data.OrderId, model.Id);
    }

    [Fact]
    public async Task BuyerDetails_ReturnsNotFound_ForAnotherBuyersOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 13, 0, 0, DateTimeKind.Utc));

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
            title: "Hidden Buyer Order");

        var createResult = await orderService.Create(
            MarketplaceTestData.CreateOrderModel((listing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "buyer-2";
        currentUserService.Username = "buyer-2";

        var controller = new UserOrdersController(orderService);
        var response = await controller.Details(
            createResult.Data!.OrderId,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(response.Result);
    }

    [Fact]
    public async Task SellerSoldDetails_ReturnsOk_ForOwnSoldOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 14, 0, 0, DateTimeKind.Utc));

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
            title: "Seller A Order");

        var sellerAOrder = await orderService.Create(
            MarketplaceTestData.CreateOrderModel((sellerAListing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "seller-a";
        currentUserService.Username = "seller-a";

        var controller = new UserOrdersController(orderService);
        var response = await controller.SoldDetails(
            sellerAOrder.Data!.OrderId,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var model = Assert.IsType<SellerOrderServiceModel>(okResult.Value);
        Assert.Equal(sellerAOrder.Data.OrderId, model.Id);
        Assert.Single(model.Items);
    }

    [Fact]
    public async Task SellerSoldDetails_ReturnsNotFound_ForAnotherSellersOrder()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "buyer-1",
            Username = "buyer-1",
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 15, 0, 0, DateTimeKind.Utc));

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
            title: "Seller A Order");

        var sellerBListing = await SeedApprovedListing(
            data,
            sellerId: "seller-b",
            title: "Seller B Order");

        await orderService.Create(
            MarketplaceTestData.CreateOrderModel((sellerAListing.Id, 1)),
            CancellationToken.None);

        var sellerBOrder = await orderService.Create(
            MarketplaceTestData.CreateOrderModel((sellerBListing.Id, 1)),
            CancellationToken.None);

        currentUserService.UserId = "seller-a";
        currentUserService.Username = "seller-a";

        var controller = new UserOrdersController(orderService);
        var response = await controller.SoldDetails(
            sellerBOrder.Data!.OrderId,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(response.Result);
    }

    [Fact]
    public async Task AdminDetails_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "admin-1",
            Username = "admin-1",
            Admin = true,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 16, 0, 0, DateTimeKind.Utc));

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

        var controller = new AdminOrdersController(orderService);
        var response = await controller.Details(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(response.Result);
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
            title: title,
            author: "Order Access Author");

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
}
