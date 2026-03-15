namespace BookStack.Features.BookListings.Service.Models;

using Shared;

using static Common.Constants;

public class BookListingFilterServiceModel
{
    public string? SearchTerm { get; init; }

    public Guid? BookId { get; init; }

    public string? Title { get; init; }

    public string? Author { get; init; }

    public string? Genre { get; init; }

    public string? Publisher { get; init; }

    public string? Isbn { get; init; }

    public string? CreatorId { get; init; }

    public ListingCondition? Condition { get; init; }

    public decimal? PriceFrom { get; init; }

    public decimal? PriceTo { get; init; }

    public bool? IsApproved { get; init; }

    public DateOnly? PublishedFrom { get; init; }

    public DateOnly? PublishedTo { get; init; }

    public int PageIndex { get; init; } = DefaultValues.DefaultPageIndex;

    public int PageSize { get; init; } = DefaultValues.DefaultPageSize;

    public ListingSorting Sorting { get; init; } = ListingSorting.Newest;
}
