namespace BookStack.Features.Orders.Service.Models;

using Shared;

public class CreateOrderServiceModel
{
    public string CustomerFirstName { get; init; } = default!;

    public string CustomerLastName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string? PhoneNumber { get; init; }

    public string Country { get; init; } = default!;

    public string City { get; init; } = default!;

    public string AddressLine { get; init; } = default!;

    public string? PostalCode { get; init; }

    public OrderPaymentMethod PaymentMethod { get; init; }

    public IEnumerable<CreateOrderItemServiceModel> Items { get; init; } = [];
}
