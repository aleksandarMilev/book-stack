namespace BookStack.Features.BookListings.Web.Models;

using System.ComponentModel.DataAnnotations;
using Shared;

using static Common.Constants.DefaultValues;

public class BookListingFilterWebModel
{
    public string? SearchTerm { get; init; }

    public Guid? BookId { get; init; }

    public string? Title { get; init; }

    public string? Author { get; init; }

    public string? Genre { get; init; }

    public string? Publisher { get; init; }

    public string? Isbn { get; init; }

    public ListingCondition? Condition { get; init; }

    public decimal? PriceFrom { get; init; }

    public decimal? PriceTo { get; init; }

    public bool? IsApproved { get; init; }

    public DateOnly? PublishedFrom { get; init; }

    public DateOnly? PublishedTo { get; init; }

    [Range(1, int.MaxValue)]
    public int PageIndex { get; init; } = DefaultPageIndex;

    [Range(1, 100)]
    public int PageSize { get; init; } = DefaultPageSize;

    public ListingSorting Sorting { get; init; } = ListingSorting.Newest;
}
