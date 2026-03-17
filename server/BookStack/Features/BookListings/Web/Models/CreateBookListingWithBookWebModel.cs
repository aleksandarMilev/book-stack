namespace BookStack.Features.BookListings.Web.Models;

using System.ComponentModel.DataAnnotations;
using BookStack.Features.BookListings.Shared;
using BookStack.Infrastructure.Validation;

using static BookStack.Features.BookListings.Shared.Constants;

public class CreateBookListingWithBookWebModel
{
    [Required]
    [StringLength(
        BookStack.Features.Books.Shared.Constants.Validation.TitleMaxLength,
        MinimumLength = BookStack.Features.Books.Shared.Constants.Validation.TitleMinLength)]
    public string Title { get; init; } = default!;

    [Required]
    [StringLength(
        BookStack.Features.Books.Shared.Constants.Validation.AuthorMaxLength,
        MinimumLength = BookStack.Features.Books.Shared.Constants.Validation.AuthorMinLength)]
    public string Author { get; init; } = default!;

    [Required]
    [StringLength(
        BookStack.Features.Books.Shared.Constants.Validation.GenreMaxLength,
        MinimumLength = BookStack.Features.Books.Shared.Constants.Validation.GenreMinLength)]
    public string Genre { get; init; } = default!;

    [StringLength(
        BookStack.Features.Books.Shared.Constants.Validation.DescriptionMaxLength,
        MinimumLength = BookStack.Features.Books.Shared.Constants.Validation.DescriptionMinLength)]
    public string? BookDescription { get; init; }

    [StringLength(
        BookStack.Features.Books.Shared.Constants.Validation.PublisherMaxLength,
        MinimumLength = BookStack.Features.Books.Shared.Constants.Validation.PublisherMinLength)]
    public string? Publisher { get; init; }

    public DateOnly? PublishedOn { get; init; }

    [StringLength(
        BookStack.Features.Books.Shared.Constants.Validation.IsbnMaxLength,
        MinimumLength = BookStack.Features.Books.Shared.Constants.Validation.IsbnMinLength)]
    public string? Isbn { get; init; }

    [Range(typeof(decimal), "0.01", "100000")]
    public decimal Price { get; init; }

    [Required]
    [StringLength(Validation.CurrencyMaxLength, MinimumLength = Validation.CurrencyMinLength)]
    public string Currency { get; init; } = default!;

    [Required]
    public ListingCondition Condition { get; init; }

    [Range(Validation.MinQuantity, Validation.MaxQuantity)]
    public int Quantity { get; init; }

    [Required]
    [StringLength(Validation.DescriptionMaxLength, MinimumLength = Validation.DescriptionMinLength)]
    public string Description { get; init; } = default!;

    [ImageUpload]
    public IFormFile? Image { get; init; }
}
