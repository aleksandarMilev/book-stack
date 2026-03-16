namespace BookStack.Features.Payments.Service.Models;

public class PaymentProviderCheckoutRequestServiceModel
{
    public Guid OrderId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = default!;

    public string Email { get; init; } = default!;
}
