namespace BookStack.Features.Orders.Service.Models;

public class CreateOrderItemServiceModel
{
    public Guid ListingId { get; init; }

    public int Quantity { get; init; }
}
