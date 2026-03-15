namespace BookStack.Features.Orders.Service.Models;

using Shared;

using static Common.Constants;

public class OrderFilterServiceModel
{
    public string? SearchTerm { get; init; }

    public string? BuyerId { get; init; }

    public string? Email { get; init; }

    public OrderStatus? Status { get; init; }

    public PaymentStatus? PaymentStatus { get; init; }

    public int PageIndex { get; init; } = DefaultValues.DefaultPageIndex;

    public int PageSize { get; init; } = DefaultValues.DefaultPageSize;
}
