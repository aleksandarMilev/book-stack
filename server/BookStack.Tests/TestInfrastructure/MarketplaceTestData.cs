namespace BookStack.Tests.TestInfrastructure;

using System.Text.Json;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Features.BookListings.Shared;
using BookStack.Features.Books.Data.Models;
using BookStack.Features.Books.Service.Models;
using BookStack.Features.Books.Shared;
using BookStack.Features.Orders.Data.Models;
using BookStack.Features.Orders.Service.Models;
using BookStack.Features.Orders.Shared;

internal static class MarketplaceTestData
{
    public static BookDbModel CreateApprovedBook(
        string creatorId,
        string title,
        string author,
        string? isbn = null,
        string genre = "Fantasy",
        bool isApproved = true)
        => new()
        {
            Title = title,
            Author = author,
            NormalizedTitle = BookMapping.NormalizeIdentityText(title),
            NormalizedAuthor = BookMapping.NormalizeIdentityText(author),
            Genre = genre,
            Description = "Test description",
            Publisher = "Test publisher",
            PublishedOn = new DateOnly(2007, 07, 21),
            Isbn = isbn,
            NormalizedIsbn = BookMapping.NormalizeIdentityIsbn(isbn),
            CreatorId = creatorId,
            IsApproved = isApproved,
        };

    public static BookListingDbModel CreateApprovedListing(
        Guid bookId,
        string creatorId,
        decimal price = 20m,
        int quantity = 5,
        string currency = "USD",
        bool isApproved = true)
        => new()
        {
            BookId = bookId,
            CreatorId = creatorId,
            Price = price,
            Currency = currency,
            Condition = ListingCondition.New,
            Quantity = quantity,
            Description = "Listing description",
            ImagePath = "/images/test-cover.jpg",
            IsApproved = isApproved,
        };

    public static CreateBookServiceModel CreateBookModel(
        string title,
        string author,
        string? isbn = null)
        => new()
        {
            Title = title,
            Author = author,
            Genre = "Fantasy",
            Description = "Canonical book",
            Publisher = "Publisher",
            PublishedOn = new DateOnly(2000, 01, 01),
            Isbn = isbn,
        };

    public static CreateOrderServiceModel CreateOrderModel(
        params (Guid ListingId, int Quantity)[] items)
        => new()
        {
            CustomerFirstName = "John",
            CustomerLastName = "Buyer",
            Email = "buyer@example.com",
            PhoneNumber = "+12025550123",
            Country = "US",
            City = "New York",
            AddressLine = "5th Avenue 1",
            PostalCode = "10001",
            Items = items.Select(static item => new CreateOrderItemServiceModel
            {
                ListingId = item.ListingId,
                Quantity = item.Quantity,
            }),
        };

    public static OrderDbModel CreateOrderDbModel(
        string? buyerId,
        decimal totalAmount,
        string currency,
        OrderStatus status,
        PaymentStatus paymentStatus,
        DateTime reservationExpiresOnUtc)
        => new()
        {
            BuyerId = buyerId,
            CustomerFirstName = "Jane",
            CustomerLastName = "Buyer",
            Email = "jane.buyer@example.com",
            PhoneNumber = "+12025550124",
            Country = "US",
            City = "Boston",
            AddressLine = "Main Street 10",
            PostalCode = "02110",
            TotalAmount = totalAmount,
            Currency = currency,
            Status = status,
            PaymentStatus = paymentStatus,
            ReservationExpiresOnUtc = reservationExpiresOnUtc,
        };

    public static string CreateMockWebhookPayload(
        string eventId,
        string paymentSessionId,
        string status,
        DateTime occurredOnUtc,
        string? failureReason = null)
        => JsonSerializer.Serialize(new
        {
            EventId = eventId,
            PaymentSessionId = paymentSessionId,
            Status = status,
            FailureReason = failureReason,
            OccurredOnUtc = occurredOnUtc,
        });
}
