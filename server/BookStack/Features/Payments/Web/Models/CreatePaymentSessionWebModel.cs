namespace BookStack.Features.Payments.Web.Models;

public class CreatePaymentSessionWebModel
{
    public string? Provider { get; init; }

    public string? PaymentToken { get; init; }
}
