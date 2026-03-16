namespace BookStack.Features.Orders.Service.Models;

using BookListings.Shared;

public class SellerOrderItemServiceModel
{
    public Guid Id { get; init; }

    public Guid ListingId { get; init; }

    public Guid BookId { get; init; }

    public string BookTitle { get; init; } = default!;

    public string BookAuthor { get; init; } = default!;

    public string BookGenre { get; init; } = default!;

    public string? BookPublisher { get; init; }

    public string? BookPublishedOn { get; init; }

    public string? BookIsbn { get; init; }

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public decimal TotalPrice { get; init; }

    public string Currency { get; init; } = default!;

    public ListingCondition Condition { get; init; }

    public string ListingDescription { get; init; } = default!;

    public string ListingImagePath { get; init; } = default!;
}
