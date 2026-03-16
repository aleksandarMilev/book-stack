namespace BookStack.Features.Payments.Shared;

public enum ReservationReleaseReason
{
    PaymentFailedOrCanceled = 0,
    OrderCanceled = 1,
    Expired = 2,

}
