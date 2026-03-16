namespace BookStack.Features.Payments.Web.Models;

using Shared;

public class PaymentCheckoutSessionWebModel
{
    public Guid PaymentId { get; init; }

    public Guid OrderId { get; init; }

    public string Provider { get; init; } = default!;

    public string ProviderPaymentId { get; init; } = default!;

    public string CheckoutUrl { get; init; } = default!;

    public PaymentRecordStatus Status { get; init; }
}
