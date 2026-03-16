namespace BookStack.Features.Orders.Service.Models;

using Shared;

public class SellerOrderServiceModel
{
    public Guid Id { get; init; }

    // Sellers receive fulfillment details intentionally to ship sold items.
    public string CustomerFirstName { get; init; } = default!;

    public string CustomerLastName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string? PhoneNumber { get; init; }

    public string Country { get; init; } = default!;

    public string City { get; init; } = default!;

    public string AddressLine { get; init; } = default!;

    public string? PostalCode { get; init; }

    public decimal SellerTotalAmount { get; init; }

    public string Currency { get; init; } = default!;

    public OrderStatus Status { get; init; }

    public PaymentStatus PaymentStatus { get; init; }

    public string CreatedOn { get; init; } = default!;

    public IEnumerable<SellerOrderItemServiceModel> Items { get; init; } = [];
}
