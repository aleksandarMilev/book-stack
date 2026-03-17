namespace BookStack.Features.Orders.Shared;

public enum OrderStatus
{
    PendingPayment = 0,
    Confirmed = 1,
    Cancelled = 2,
    Completed = 3,
    Expired = 4,
    PendingConfirmation = 5,
    Shipped = 6,
    Delivered = 7,
}
