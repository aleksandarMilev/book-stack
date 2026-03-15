namespace BookStack.Features.Orders.Shared;

public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3,
}
