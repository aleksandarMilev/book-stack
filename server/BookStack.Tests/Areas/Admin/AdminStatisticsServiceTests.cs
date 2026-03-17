namespace BookStack.Tests.Areas.Admin;

using BookStack.Data;
using BookStack.Features.Orders.Shared;
using BookStack.Features.Statistics.Service;
using BookStack.Tests.TestInfrastructure;

public class AdminStatisticsServiceTests
{
    [Fact]
    public async Task Get_ComputesBusinessCountsAndFeeBasedMonthlyMetrics()
    {
        await using var database = new TestDatabaseScope();

        var currentUserService = new TestCurrentUserService
        {
            UserId = "admin-1",
            Username = "admin-1",
            Admin = true,
        };

        var utc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = new StatisticsService(data);

        data.Users.AddRange(
            MarketplaceTestData.CreateUser("seller-1", "seller-1@example.com"),
            MarketplaceTestData.CreateUser("seller-2", "seller-2@example.com"));

        data.SellerProfiles.AddRange(
            MarketplaceTestData.CreateSellerProfile(
                userId: "seller-1",
                isActive: true),
            MarketplaceTestData.CreateSellerProfile(
                userId: "seller-2",
                isActive: false));

        await data.SaveChangesAsync(CancellationToken.None);

        var approvedBook = MarketplaceTestData.CreateApprovedBook(
            creatorId: "seller-1",
            title: "Approved Book",
            author: "Author 1",
            isApproved: true);

        var pendingBook = MarketplaceTestData.CreateApprovedBook(
            creatorId: "seller-2",
            title: "Pending Book",
            author: "Author 2",
            isApproved: false);

        data.AddRange(approvedBook, pendingBook);
        await data.SaveChangesAsync(CancellationToken.None);

        var approvedListing = MarketplaceTestData.CreateApprovedListing(
            approvedBook.Id,
            creatorId: "seller-1",
            isApproved: true);

        var pendingListing = MarketplaceTestData.CreateApprovedListing(
            pendingBook.Id,
            creatorId: "seller-2",
            isApproved: false);

        data.AddRange(
            approvedListing,
            pendingListing);

        await data.SaveChangesAsync(CancellationToken.None);

        await AddOrder(
            data,
            dateTimeProvider,
            createdOnUtc: new DateTime(2026, 01, 15, 0, 0, 0, DateTimeKind.Utc),
            totalAmount: 100m,
            currency: "USD",
            paymentMethod: OrderPaymentMethod.Online,
            paymentStatus: PaymentStatus.Paid);

        await AddOrder(
            data,
            dateTimeProvider,
            createdOnUtc: new DateTime(2026, 01, 20, 0, 0, 0, DateTimeKind.Utc),
            totalAmount: 50m,
            currency: "EUR",
            paymentMethod: OrderPaymentMethod.CashOnDelivery,
            paymentStatus: PaymentStatus.NotRequired);

        await AddOrder(
            data,
            dateTimeProvider,
            createdOnUtc: new DateTime(2026, 02, 05, 0, 0, 0, DateTimeKind.Utc),
            totalAmount: 25m,
            currency: "USD",
            paymentMethod: OrderPaymentMethod.Online,
            paymentStatus: PaymentStatus.Paid);

        await AddOrder(
            data,
            dateTimeProvider,
            createdOnUtc: new DateTime(2026, 02, 10, 0, 0, 0, DateTimeKind.Utc),
            totalAmount: 75m,
            currency: "USD",
            paymentMethod: OrderPaymentMethod.Online,
            paymentStatus: PaymentStatus.Pending);


        var deletedPaidOrder = MarketplaceTestData.CreateOrderDbModel(
            buyerId: "buyer-1",
            totalAmount: 999m,
            currency: "USD",
            paymentMethod: OrderPaymentMethod.Online,
            status: OrderStatus.PendingConfirmation,
            paymentStatus: PaymentStatus.Paid,
            reservationExpiresOnUtc: new DateTime(2026, 02, 11, 0, 30, 0, DateTimeKind.Utc));

        deletedPaidOrder.IsDeleted = true;
        deletedPaidOrder.DeletedOn = new DateTime(2026, 02, 11, 1, 0, 0, DateTimeKind.Utc);

        data.Add(deletedPaidOrder);

        await data.SaveChangesAsync(CancellationToken.None);

        var result = await service.Get();
        var revenue = result.RevenueByMonth.ToList();

        Assert.Equal(2, result.TotalUsers);
        Assert.Equal(2, result.TotalSellerProfiles);
        Assert.Equal(1, result.ActiveSellerProfiles);
        Assert.Equal(2, result.TotalBooks);
        Assert.Equal(2, result.TotalListings);
        Assert.Equal(1, result.PendingBooks);
        Assert.Equal(1, result.PendingListings);
        Assert.Equal(4, result.TotalOrders);
        Assert.Equal(2, result.PaidOnlineOrders);
        Assert.Equal(1, result.CodOrders);
        Assert.Equal(3, revenue.Count);

        Assert.Contains(revenue, static item =>
            item.Year == 2026 &&
            item.Month == 1 &&
            item.Currency == "USD" &&
            item.GrossRevenue == 100m &&
            item.PlatformFeeRevenue == 10m &&
            item.SellerNetRevenue == 90m &&
            item.Orders == 1 &&
            item.PaidOnlineOrders == 1 &&
            item.CodOrders == 0);

        Assert.Contains(revenue, static item =>
            item.Year == 2026 &&
            item.Month == 1 &&
            item.Currency == "EUR" &&
            item.GrossRevenue == 50m &&
            item.PlatformFeeRevenue == 5m &&
            item.SellerNetRevenue == 45m &&
            item.Orders == 1 &&
            item.PaidOnlineOrders == 0 &&
            item.CodOrders == 1);

        Assert.Contains(revenue, static item =>
            item.Year == 2026 &&
            item.Month == 2 &&
            item.Currency == "USD" &&
            item.GrossRevenue == 100m &&
            item.PlatformFeeRevenue == 10m &&
            item.SellerNetRevenue == 90m &&
            item.Orders == 2 &&
            item.PaidOnlineOrders == 1 &&
            item.CodOrders == 0);
    }

    private static async Task AddOrder(
        BookStackDbContext data,
        TestDateTimeProvider dateTimeProvider,
        DateTime createdOnUtc,
        decimal totalAmount,
        string currency,
        OrderPaymentMethod paymentMethod,
        PaymentStatus paymentStatus)
    {
        dateTimeProvider.UtcNow = createdOnUtc;

        var order = MarketplaceTestData.CreateOrderDbModel(
            buyerId: "buyer-1",
            totalAmount: totalAmount,
            currency: currency,
            paymentMethod: paymentMethod,
            status: paymentMethod == OrderPaymentMethod.Online
                ? (paymentStatus == PaymentStatus.Paid
                    ? OrderStatus.PendingConfirmation
                    : OrderStatus.PendingPayment)
                : OrderStatus.PendingConfirmation,
            paymentStatus: paymentStatus,
            reservationExpiresOnUtc: createdOnUtc.AddMinutes(30));

        data.Add(order);
        await data.SaveChangesAsync(CancellationToken.None);
    }
}
