namespace BookStack.Features.Orders.Web.Models;

using System.ComponentModel.DataAnnotations;
using Shared;

using static Common.Constants.DefaultValues;

public class OrderFilterWebModel
{
    public string? SearchTerm { get; init; }

    public string? Email { get; init; }

    public OrderStatus? Status { get; init; }

    public PaymentStatus? PaymentStatus { get; init; }

    [Range(1, int.MaxValue)]
    public int PageIndex { get; init; } = DefaultPageIndex;

    [Range(1, 100)]
    public int PageSize { get; init; } = DefaultPageSize;
}
