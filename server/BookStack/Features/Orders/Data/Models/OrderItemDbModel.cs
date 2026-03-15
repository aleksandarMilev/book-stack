namespace BookStack.Features.Orders.Data.Models;

using BookStack.Data.Models.Base;
using BookListings.Data.Models;
using BookListings.Shared;
using Books.Data.Models;

public class OrderItemDbModel : DeletableEntity<Guid>
{
    public Guid OrderId { get; set; }

    public OrderDbModel Order { get; set; } = default!;

    public Guid ListingId { get; set; }

    public BookListingDbModel Listing { get; set; } = default!;

    public Guid BookId { get; set; }

    public BookDbModel Book { get; set; } = default!;

    public string SellerId { get; set; } = default!;

    public string BookTitle { get; set; } = default!;

    public string BookAuthor { get; set; } = default!;

    public string BookGenre { get; set; } = default!;

    public string? BookPublisher { get; set; }

    public string? BookPublishedOn { get; set; }

    public string? BookIsbn { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public string Currency { get; set; } = default!;

    public ListingCondition Condition { get; set; }

    public string ListingDescription { get; set; } = default!;

    public string ListingImagePath { get; set; } = default!;
}
