namespace BookStack.Features.BookListings.Web.Models;

using BookStack.Features.BookListings.Shared;
using BookStack.Infrastructure.Validation;
using System.ComponentModel.DataAnnotations;

using static Shared.Constants.Validation;

public class CreateBookListingWebModel
{
    [Required]
    public Guid BookId { get; init; }

    [Range(typeof(decimal), "0.01", "100000")]
    public decimal Price { get; init; }

    [Required]
    [StringLength(CurrencyMaxLength, MinimumLength = CurrencyMinLength)]
    public string Currency { get; init; } = default!;

    [Required]
    public ListingCondition Condition { get; init; }

    [Range(MinQuantity, MaxQuantity)]
    public int Quantity { get; init; }

    [Required]
    [StringLength(DescriptionMaxLength, MinimumLength = DescriptionMinLength)]
    public string Description { get; init; } = default!;

    [ImageUpload]
    public IFormFile? Image { get; init; }

    public bool RemoveImage { get; init; }
}
