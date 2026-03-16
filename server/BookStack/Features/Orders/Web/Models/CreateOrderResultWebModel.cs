namespace BookStack.Features.Orders.Web.Models;

public class CreateOrderResultWebModel
{
    public Guid OrderId { get; init; }

    public string? PaymentToken { get; init; }
}
