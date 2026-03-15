namespace BookStack.Features.BookListings.Service.Models;

using Shared;

public class BookListingLookupServiceModel
{
    public Guid Id { get; init; }

    public Guid BookId { get; init; }

    public string BookTitle { get; init; } = default!;

    public string BookAuthor { get; init; } = default!;

    public decimal Price { get; init; }

    public string Currency { get; init; } = default!;

    public ListingCondition Condition { get; init; }
}
