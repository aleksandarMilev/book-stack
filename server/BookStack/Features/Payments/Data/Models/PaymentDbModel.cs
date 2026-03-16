namespace BookStack.Features.Payments.Data.Models;

using BookStack.Data.Models.Base;
using Orders.Data.Models;
using Shared;

public class PaymentDbModel : DeletableEntity<Guid>
{
    public Guid OrderId { get; set; }

    public OrderDbModel Order { get; set; } = default!;

    public string Provider { get; set; } = default!;

    public string ProviderPaymentId { get; set; } = default!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = default!;

    public PaymentRecordStatus Status { get; set; }

    public string? FailureReason { get; set; }

    public string? LastProviderEventId { get; set; }

    public DateTime? LastEventOnUtc { get; set; }

    public DateTime? CompletedOnUtc { get; set; }

    public DateTime? FailedOnUtc { get; set; }

    public DateTime? RefundedOnUtc { get; set; }

    public DateTime? CanceledOnUtc { get; set; }
}
