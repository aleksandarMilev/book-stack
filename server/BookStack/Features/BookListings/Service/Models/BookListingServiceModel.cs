namespace BookStack.Features.BookListings.Service.Models;

using Shared;

public class BookListingServiceModel
{
    public Guid Id { get; init; }

    public Guid BookId { get; init; }

    public string BookTitle { get; init; } = default!;

    public string BookAuthor { get; init; } = default!;

    public string BookGenre { get; init; } = default!;

    public string? BookPublisher { get; init; }

    public string? BookPublishedOn { get; init; }

    public string? BookIsbn { get; init; }

    public string CreatorId { get; init; } = default!;

    public decimal Price { get; init; }

    public string Currency { get; init; } = default!;

    public ListingCondition Condition { get; init; }

    public int Quantity { get; init; }

    public string Description { get; init; } = default!;

    public string ImagePath { get; init; } = default!;

    public bool IsApproved { get; init; }

    public string? RejectionReason { get; init; }

    public string CreatedOn { get; init; } = default!;

    public string? ModifiedOn { get; init; }

    public string? ApprovedOn { get; init; }

    public string? ApprovedBy { get; init; }
}
