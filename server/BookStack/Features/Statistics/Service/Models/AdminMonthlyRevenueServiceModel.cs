namespace BookStack.Features.Statistics.Service.Models;

public class AdminMonthlyRevenueServiceModel
{
    public int Year { get; init; }

    public int Month { get; init; }

    public string Currency { get; init; } = default!;

    public decimal Revenue { get; init; }

    public int PaidOrders { get; init; }
}
