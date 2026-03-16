namespace BookStack.Features.BookListings.Shared;

using Common;
using Data.Models;
using Service.Models;
using Web.Models;

using static Common.Constants;

public static class BookListingMapping
{
    public static IQueryable<BookListingServiceModel> ToServiceModels(
        this IQueryable<BookListingDbModel> dbModels)
        => dbModels.Select(static l => new BookListingServiceModel
        {
            Id = l.Id,
            BookId = l.BookId,
            BookTitle = l.Book.Title,
            BookAuthor = l.Book.Author,
            BookGenre = l.Book.Genre,
            BookPublisher = l.Book.Publisher,
            BookPublishedOn = l.Book.PublishedOn.HasValue
                ? l.Book.PublishedOn.Value.ToString(DateFormats.ISO8601)
                : null,
            BookIsbn = l.Book.Isbn,
            CreatorId = l.CreatorId,
            Price = l.Price,
            Currency = l.Currency,
            Condition = l.Condition,
            Quantity = l.Quantity,
            Description = l.Description,
            ImagePath = l.ImagePath,
            IsApproved = l.IsApproved,
            RejectionReason = l.RejectionReason,
            CreatedOn = l.CreatedOn.ToString("O"),
            ModifiedOn = l.ModifiedOn.ToIso8601String(),
            ApprovedOn = l.ApprovedOn.ToIso8601String(),
            ApprovedBy = l.ApprovedBy,
        });

    public static IQueryable<BookListingLookupServiceModel> ToLookupServiceModels(
        this IQueryable<BookListingDbModel> dbModels)
        => dbModels.Select(static l => new BookListingLookupServiceModel
        {
            Id = l.Id,
            BookId = l.BookId,
            BookTitle = l.Book.Title,
            BookAuthor = l.Book.Author,
            Price = l.Price,
            Currency = l.Currency,
            Condition = l.Condition,
        });

    public static BookListingDbModel ToDbModel(
        this CreateBookListingServiceModel serviceModel,
        string creatorId)
        => new()
        {
            BookId = serviceModel.BookId,
            CreatorId = creatorId,
            Price = serviceModel.Price,
            Currency = serviceModel.Currency.Trim().ToUpperInvariant(),
            Condition = serviceModel.Condition,
            Quantity = serviceModel.Quantity,
            Description = serviceModel.Description.Trim(),
            IsApproved = false,
            ApprovedOn = null,
            ApprovedBy = null,
            RejectionReason = null,
        };

    public static void UpdateDbModel(
        this CreateBookListingServiceModel serviceModel,
        BookListingDbModel dbModel)
    {
        dbModel.BookId = serviceModel.BookId;
        dbModel.Price = serviceModel.Price;
        dbModel.Currency = serviceModel.Currency.Trim().ToUpperInvariant();
        dbModel.Condition = serviceModel.Condition;
        dbModel.Quantity = serviceModel.Quantity;
        dbModel.Description = serviceModel.Description.Trim();
        dbModel.IsApproved = false;
        dbModel.ApprovedOn = null;
        dbModel.ApprovedBy = null;
        dbModel.RejectionReason = null;
    }

    public static CreateBookListingServiceModel ToCreateServiceModel(
        this CreateBookListingWebModel webModel)
        => new()
        {
            BookId = webModel.BookId,
            Price = webModel.Price,
            Currency = webModel.Currency,
            Condition = webModel.Condition,
            Quantity = webModel.Quantity,
            Description = webModel.Description,
            Image = webModel.Image,
            RemoveImage = webModel.RemoveImage,
        };

    public static BookListingFilterServiceModel ToFilterServiceModel(
        this BookListingFilterWebModel webModel,
        string? creatorId = null)
        => new()
        {
            SearchTerm = webModel.SearchTerm,
            BookId = webModel.BookId,
            Title = webModel.Title,
            Author = webModel.Author,
            Genre = webModel.Genre,
            Publisher = webModel.Publisher,
            Isbn = webModel.Isbn,
            CreatorId = creatorId,
            Condition = webModel.Condition,
            PriceFrom = webModel.PriceFrom,
            PriceTo = webModel.PriceTo,
            IsApproved = webModel.IsApproved,
            PublishedFrom = webModel.PublishedFrom,
            PublishedTo = webModel.PublishedTo,
            PageIndex = webModel.PageIndex,
            PageSize = webModel.PageSize,
            Sorting = webModel.Sorting,
        };
}
