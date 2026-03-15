namespace BookStack.Features.Books.Service.Models;

using Shared;

using static Common.Constants;

public class BookFilterServiceModel
{
    public string? SearchTerm { get; init; }

    public string? Title { get; init; }

    public string? Author { get; init; }

    public string? Genre { get; init; }

    public string? Publisher { get; init; }

    public string? Isbn { get; init; }

    public string? CreatorId { get; init; }

    public bool? IsApproved { get; init; }

    public DateOnly? PublishedFrom { get; init; }

    public DateOnly? PublishedTo { get; init; }

    public int PageIndex { get; init; } = DefaultValues.DefaultPageIndex;

    public int PageSize { get; init; } = DefaultValues.DefaultPageSize;

    public BookSorting Sorting { get; init; } = BookSorting.Newest;
}
