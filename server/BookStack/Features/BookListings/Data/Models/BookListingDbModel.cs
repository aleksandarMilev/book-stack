namespace BookStack.Features.BookListings.Data.Models;

using Books.Data.Models;
using BookStack.Data.Models.Base;
using Infrastructure.Services.ImageWriter.Models;
using Shared;

public class BookListingDbModel:
    DeletableEntity<Guid>,
    IApprovableEntity,
    IImageDbModel
{
    public Guid BookId { get; set; }

    public BookDbModel Book { get; set; } = default!;

    public string CreatorId { get; set; } = default!;

    public decimal Price { get; set; }

    public string Currency { get; set; } = default!;

    public ListingCondition Condition { get; set; }

    public int Quantity { get; set; }

    public string Description { get; set; } = default!;

    public string ImagePath { get; set; } = default!;

    public bool IsApproved { get; set; }

    public DateTime? ApprovedOn { get; set; }

    public string? ApprovedBy { get; set; }

    public string? RejectionReason { get; set; }
}
