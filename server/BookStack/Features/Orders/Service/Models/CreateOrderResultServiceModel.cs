namespace BookStack.Features.Orders.Service.Models;

public class CreateOrderResultServiceModel
{
    public Guid OrderId { get; init; }

    public string? PaymentToken { get; init; }
}
