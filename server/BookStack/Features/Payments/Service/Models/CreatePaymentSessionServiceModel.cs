namespace BookStack.Features.Payments.Service.Models;

public class CreatePaymentSessionServiceModel
{
    public string? Provider { get; init; }

    public string? PaymentToken { get; init; }
}
