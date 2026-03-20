namespace BookStack.Features.Statistics.Service;

using Data;
using Features.Orders.Shared;
using Infrastructure.Services.CurrentUser;
using Microsoft.EntityFrameworkCore;
using Models;

public class StatisticsService(
    BookStackDbContext data,
    ICurrentUserService currentUserService,
    ILogger<StatisticsService> logger) : IStatisticsService
{
    public async Task<AdminStatisticsServiceModel> Get(
        CancellationToken cancellationToken = default)
    {
        if (!currentUserService.IsAdmin())
        {
            logger.LogWarning(
                "Unauthorized statistics access attempt. UserId={UserId}",
                currentUserService.GetId());

            return new();
        }

        var totalUsers = await data
            .Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalSellerProfiles = await data
            .SellerProfiles
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var activeSellerProfiles = await data
            .SellerProfiles
            .AsNoTracking()
            .CountAsync(
                static p => p.IsActive,
                cancellationToken);

        var totalBooks = await data
            .Books
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalListings = await data
            .BookListings
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var pendingBooks = await data
            .Books
            .AsNoTracking()
            .CountAsync(
                static b => !b.IsApproved,
                cancellationToken);

        var pendingListings = await data
            .BookListings
            .AsNoTracking()
            .CountAsync(
                static l => !l.IsApproved,
                cancellationToken);

        var ordersQuery = data
            .Orders
            .AsNoTracking();

        var totalOrders = await ordersQuery
            .CountAsync(cancellationToken);

        var paidOnlineOrders = await ordersQuery
            .CountAsync(
                static o =>
                    o.PaymentMethod == OrderPaymentMethod.Online &&
                    o.PaymentStatus == PaymentStatus.Paid,
                cancellationToken);

        var codOrders = await ordersQuery
            .CountAsync(
                static o => o.PaymentMethod == OrderPaymentMethod.CashOnDelivery,
                cancellationToken);

        var totalPendingSettlementAmount = await ordersQuery
            .SumAsync(
                static o => (
                    (
                        o.PaymentMethod == OrderPaymentMethod.Online &&
                        o.PaymentStatus == PaymentStatus.Paid &&
                        o.Status != OrderStatus.Cancelled &&
                        o.Status != OrderStatus.Expired) ||
                    (
                        o.PaymentMethod == OrderPaymentMethod.CashOnDelivery &&
                        (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed)))
                    && o.SettlementStatus == SettlementStatus.Pending
                        ? o.PlatformFeeAmount
                        : 0m,
                cancellationToken);

        var revenueByMonth = await ordersQuery
            .GroupBy(static o => new
            {
                o.CreatedOn.Year,
                o.CreatedOn.Month,
                o.Currency,
            })
            .Select(static group => new AdminMonthlyRevenueServiceModel
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                Currency = group.Key.Currency,
                GrossOrderVolume = group.Sum(static o => o.TotalAmount),
                RecognizedPlatformFeeRevenue = group.Sum(o =>
                    (
                        (
                            o.PaymentMethod == OrderPaymentMethod.Online &&
                            o.PaymentStatus == PaymentStatus.Paid &&
                            o.Status != OrderStatus.Cancelled &&
                            o.Status != OrderStatus.Expired) ||
                        (
                            o.PaymentMethod == OrderPaymentMethod.CashOnDelivery &&
                            (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed))
                    )
                        ? o.PlatformFeeAmount
                        : 0m),
                RecognizedSellerNetRevenue = group.Sum(o =>
                    (
                        (
                            o.PaymentMethod == OrderPaymentMethod.Online &&
                            o.PaymentStatus == PaymentStatus.Paid &&
                            o.Status != OrderStatus.Cancelled &&
                            o.Status != OrderStatus.Expired) ||
                        (
                            o.PaymentMethod == OrderPaymentMethod.CashOnDelivery &&
                            (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed))
                    )
                        ? o.SellerNetAmount
                        : 0m),
                PendingSettlementAmount = group.Sum(o =>
                    (
                        (
                            o.PaymentMethod == OrderPaymentMethod.Online &&
                            o.PaymentStatus == PaymentStatus.Paid &&
                            o.Status != OrderStatus.Cancelled &&
                            o.Status != OrderStatus.Expired) ||
                        (
                            o.PaymentMethod == OrderPaymentMethod.CashOnDelivery &&
                            (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed))
                    ) &&
                    o.SettlementStatus == SettlementStatus.Pending
                        ? o.PlatformFeeAmount
                        : 0m),
                UnearnedPlatformFeeAmount = group.Sum(o =>
                    !(
                        (
                            o.PaymentMethod == OrderPaymentMethod.Online &&
                            o.PaymentStatus == PaymentStatus.Paid &&
                            o.Status != OrderStatus.Cancelled &&
                            o.Status != OrderStatus.Expired) ||
                        (
                            o.PaymentMethod == OrderPaymentMethod.CashOnDelivery &&
                            (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed))
                    ) &&
                    o.Status != OrderStatus.Cancelled &&
                    o.Status != OrderStatus.Expired
                        ? o.PlatformFeeAmount
                        : 0m),
                Orders = group.Count(),
                PaidOnlineOrders = group.Count(o =>
                    o.PaymentMethod == OrderPaymentMethod.Online &&
                    o.PaymentStatus == PaymentStatus.Paid),
                CodOrders = group.Count(o =>
                    o.PaymentMethod == OrderPaymentMethod.CashOnDelivery),
            })
            .OrderBy(static item => item.Year)
            .ThenBy(static item => item.Month)
            .ThenBy(static item => item.Currency)
            .ToListAsync(cancellationToken);

        return new()
        {
            TotalUsers = totalUsers,
            TotalSellerProfiles = totalSellerProfiles,
            ActiveSellerProfiles = activeSellerProfiles,
            TotalBooks = totalBooks,
            TotalListings = totalListings,
            PendingBooks = pendingBooks,
            PendingListings = pendingListings,
            TotalOrders = totalOrders,
            PaidOnlineOrders = paidOnlineOrders,
            CodOrders = codOrders,
            TotalPendingSettlementAmount = totalPendingSettlementAmount,
            RevenueByMonth = revenueByMonth,
        };
    }
}
