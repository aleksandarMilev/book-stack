namespace BookStack.Features.BookListings.Service.Models;

using BookStack.Features.Books.Service.Models;
using Infrastructure.Services.ImageWriter.Models;
using Shared;

public class CreateBookListingWithBookServiceModel : IImageServiceModel
{
    public CreateBookServiceModel Book { get; init; } = default!;

    public decimal Price { get; init; }

    public string Currency { get; init; } = default!;

    public ListingCondition Condition { get; init; }

    public int Quantity { get; init; }

    public string Description { get; init; } = default!;

    public IFormFile? Image { get; init; }

    public bool RemoveImage { get; init; }
}
