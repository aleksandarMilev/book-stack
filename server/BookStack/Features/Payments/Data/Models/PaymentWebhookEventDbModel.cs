namespace BookStack.Features.Payments.Data.Models;

using BookStack.Data.Models.Base;
using Orders.Data.Models;
using Shared;

public class PaymentWebhookEventDbModel : DeletableEntity<Guid>
{
    public string Provider { get; set; } = default!;

    public string ProviderEventId { get; set; } = default!;

    public string? ProviderPaymentId { get; set; }

    public Guid? PaymentId { get; set; }

    public PaymentDbModel? Payment { get; set; }

    public Guid? OrderId { get; set; }

    public OrderDbModel? Order { get; set; }

    public PaymentRecordStatus? Status { get; set; }

    public string? FailureReason { get; set; }

    public string? ProcessingResult { get; set; }

    public DateTime ProcessedOnUtc { get; set; }

    public string? Payload { get; set; }
}
