namespace BookStack.Features.Books.Web.Models;

using System.ComponentModel.DataAnnotations;
using Shared;

using static Common.Constants.DefaultValues;

public class BookFilterWebModel
{
    public string? SearchTerm { get; init; }

    public string? Title { get; init; }

    public string? Author { get; init; }

    public string? Genre { get; init; }

    public string? Publisher { get; init; }

    public string? Isbn { get; init; }

    public bool? IsApproved { get; init; }

    public DateOnly? PublishedFrom { get; init; }

    public DateOnly? PublishedTo { get; init; }

    [Range(1, int.MaxValue)]
    public int PageIndex { get; init; } = DefaultPageIndex;

    [Range(1, 100)]
    public int PageSize { get; init; } = DefaultPageSize;

    public BookSorting Sorting { get; init; } = BookSorting.Newest;
}