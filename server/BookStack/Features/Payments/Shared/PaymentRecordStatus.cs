namespace BookStack.Features.Payments.Shared;

public enum PaymentRecordStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4,
    Canceled = 5,
}
