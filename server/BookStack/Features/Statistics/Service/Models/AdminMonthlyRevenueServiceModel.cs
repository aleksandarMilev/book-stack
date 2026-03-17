namespace BookStack.Features.Statistics.Service.Models;

public class AdminMonthlyRevenueServiceModel
{
    public int Year { get; init; }

    public int Month { get; init; }

    public string Currency { get; init; } = default!;

    public decimal GrossOrderVolume { get; init; }

    public decimal RecognizedPlatformFeeRevenue { get; init; }

    public decimal RecognizedSellerNetRevenue { get; init; }

    public decimal PendingSettlementAmount { get; init; }

    public decimal UnearnedPlatformFeeAmount { get; init; }

    public int Orders { get; init; }

    public int PaidOnlineOrders { get; init; }

    public int CodOrders { get; init; }
}
