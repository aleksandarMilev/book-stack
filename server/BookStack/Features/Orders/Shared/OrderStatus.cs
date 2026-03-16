namespace BookStack.Features.Orders.Shared;

public enum OrderStatus
{
    PendingPayment = 0,
    Pending = PendingPayment,
    Confirmed = 1,
    Cancelled = 2,
    Completed = 3,
    Expired = 4,
}
