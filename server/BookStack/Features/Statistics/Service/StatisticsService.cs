namespace BookStack.Features.Statistics.Service;

using Data;
using Features.Orders.Shared;
using Microsoft.EntityFrameworkCore;
using Models;

public class StatisticsService(
    BookStackDbContext data) : IStatisticsService
{
    private readonly BookStackDbContext _data = data;

    public async Task<AdminStatisticsServiceModel> Get(
        CancellationToken cancellationToken = default)
    {
        var totalUsers = await this._data
            .Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalBooks = await this._data
            .Books
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalListings = await this._data
            .BookListings
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var pendingBooks = await this._data
            .Books
            .AsNoTracking()
            .CountAsync(
                static b => !b.IsApproved,
                cancellationToken);

        var pendingListings = await this._data
            .BookListings
            .AsNoTracking()
            .CountAsync(
                static l => !l.IsApproved,
                cancellationToken);

        var ordersQuery = this._data
            .Orders
            .AsNoTracking();

        var totalOrders = await ordersQuery
            .CountAsync(cancellationToken);

        var paidOrders = await ordersQuery
            .CountAsync(
                static o => o.PaymentStatus == PaymentStatus.Paid,
                cancellationToken);

        var revenueByMonth = await ordersQuery
            .Where(static o => o.PaymentStatus == PaymentStatus.Paid)
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
                Revenue = group.Sum(static o => o.TotalAmount),
                PaidOrders = group.Count(),
            })
            .OrderBy(static item => item.Year)
            .ThenBy(static item => item.Month)
            .ThenBy(static item => item.Currency)
            .ToListAsync(cancellationToken);

        return new()
        {
            TotalUsers = totalUsers,
            TotalBooks = totalBooks,
            TotalListings = totalListings,
            PendingBooks = pendingBooks,
            PendingListings = pendingListings,
            TotalOrders = totalOrders,
            PaidOrders = paidOrders,
            RevenueByMonth = revenueByMonth,
        };
    }
}
