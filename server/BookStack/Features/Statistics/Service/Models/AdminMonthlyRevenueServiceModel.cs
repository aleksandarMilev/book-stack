namespace BookStack.Features.Statistics.Service.Models;

public class AdminMonthlyRevenueServiceModel
{
    public int Year { get; init; }

    public int Month { get; init; }

    public string Currency { get; init; } = default!;

    public decimal GrossRevenue { get; init; }

    public decimal PlatformFeeRevenue { get; init; }

    public decimal SellerNetRevenue { get; init; }

    public int Orders { get; init; }

    public int PaidOnlineOrders { get; init; }

    public int CodOrders { get; init; }
}
