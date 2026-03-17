namespace BookStack.Features.Orders.Data.Models;

using BookStack.Data.Models.Base;
using Shared;

public class OrderDbModel : DeletableEntity<Guid>
{
    public string? BuyerId { get; set; }

    public string CustomerFirstName { get; set; } = default!;

    public string CustomerLastName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string? PhoneNumber { get; set; }

    public string Country { get; set; } = default!;

    public string City { get; set; } = default!;

    public string AddressLine { get; set; } = default!;

    public string? PostalCode { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = default!;

    public OrderPaymentMethod PaymentMethod { get; set; }

    public OrderStatus Status { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public SettlementStatus SettlementStatus { get; set; }

    public decimal PlatformFeePercent { get; set; }

    public decimal PlatformFeeAmount { get; set; }

    public decimal SellerNetAmount { get; set; }

    public string? GuestPaymentTokenHash { get; set; }

    public DateTime ReservationExpiresOnUtc { get; set; }

    public DateTime? ReservationReleasedOnUtc { get; set; }

    public ICollection<OrderItemDbModel> Items { get; init; } = [];
}
