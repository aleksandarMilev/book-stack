namespace BookStack.Features.Orders.Service.Models;

using Shared;

public class OrderServiceModel
{
    public Guid Id { get; init; }

    public string? BuyerId { get; init; }

    public string CustomerFirstName { get; init; } = default!;

    public string CustomerLastName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string? PhoneNumber { get; init; }

    public string Country { get; init; } = default!;

    public string City { get; init; } = default!;

    public string AddressLine { get; init; } = default!;

    public string? PostalCode { get; init; }

    public decimal TotalAmount { get; init; }

    public string Currency { get; init; } = default!;

    public OrderPaymentMethod PaymentMethod { get; init; }

    public OrderStatus Status { get; init; }

    public PaymentStatus PaymentStatus { get; init; }

    public SettlementStatus SettlementStatus { get; init; }

    public decimal PlatformFeePercent { get; init; }

    public decimal PlatformFeeAmount { get; init; }

    public decimal SellerNetAmount { get; init; }

    public string CreatedOn { get; init; } = default!;

    public IEnumerable<OrderItemServiceModel> Items { get; init; } = [];
}
