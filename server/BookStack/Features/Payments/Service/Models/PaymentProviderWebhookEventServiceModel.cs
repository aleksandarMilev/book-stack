namespace BookStack.Features.Payments.Service.Models;

using Shared;

public class PaymentProviderWebhookEventServiceModel
{
    public string ProviderEventId { get; init; } = default!;

    public string ProviderPaymentId { get; init; } = default!;

    public PaymentRecordStatus Status { get; init; }

    public string? FailureReason { get; init; }

    public DateTime OccurredOnUtc { get; init; }
}
