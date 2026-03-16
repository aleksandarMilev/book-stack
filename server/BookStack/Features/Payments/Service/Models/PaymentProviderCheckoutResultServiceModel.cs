namespace BookStack.Features.Payments.Service.Models;

public class PaymentProviderCheckoutResultServiceModel
{
    public string ProviderPaymentId { get; init; } = default!;

    public string CheckoutUrl { get; init; } = default!;
}
