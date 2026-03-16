namespace BookStack.Features.Statistics.Service.Models;

public class AdminStatisticsServiceModel
{
    public int TotalUsers { get; init; }

    public int TotalBooks { get; init; }

    public int TotalListings { get; init; }

    public int PendingBooks { get; init; }

    public int PendingListings { get; init; }

    public int TotalOrders { get; init; }

    public int PaidOrders { get; init; }

    public IEnumerable<AdminMonthlyRevenueServiceModel> RevenueByMonth { get; init; } = [];
}
