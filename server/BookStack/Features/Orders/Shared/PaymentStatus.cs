namespace BookStack.Features.Orders.Shared;

public enum PaymentStatus
{
    Pending = 0,
    Unpaid = Pending,
    Paid = 1,
    Failed = 2,
    Refunded = 3,
    NotRequired = 4,
    Expired = 5,
    Cancelled = 6,
}
