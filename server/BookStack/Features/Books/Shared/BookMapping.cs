namespace BookStack.Features.Books.Shared;

using Common;
using Data.Models;
using Service.Models;
using Web.Models;

using static Common.Constants;

public static class BookMapping
{
    public static IQueryable<BookServiceModel> ToServiceModels(
        this IQueryable<BookDbModel> dbModels)
        => dbModels.Select(static b => new BookServiceModel
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            Genre = b.Genre,
            Description = b.Description,
            Publisher = b.Publisher,
            PublishedOn = b.PublishedOn.HasValue
                ? b.PublishedOn.Value.ToString(DateFormats.ISO8601)
                : null,
            Isbn = b.Isbn,
            CreatorId = b.CreatorId,
            IsApproved = b.IsApproved,
            RejectionReason = b.RejectionReason,
            CreatedOn = b.CreatedOn.ToString("O"),
            ModifiedOn = b.ModifiedOn.ToIso8601String(),
            ApprovedOn = b.ApprovedOn.ToIso8601String(),
            ApprovedBy = b.ApprovedBy,
        });

    public static IQueryable<BookLookupServiceModel> ToLookupServiceModels(
        this IQueryable<BookDbModel> dbModels)
        => dbModels.Select(static b => new BookLookupServiceModel
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            Genre = b.Genre,
            Isbn = b.Isbn,
        });

    public static BookDbModel ToDbModel(
        this CreateBookServiceModel serviceModel,
        string creatorId)
        => new()
        {
            Title = serviceModel.Title.Trim(),
            Author = serviceModel.Author.Trim(),
            Genre = serviceModel.Genre.Trim(),
            Description = string.IsNullOrWhiteSpace(serviceModel.Description)
                ? null
                : serviceModel.Description.Trim(),
            Publisher = string.IsNullOrWhiteSpace(serviceModel.Publisher)
                ? null
                : serviceModel.Publisher.Trim(),
            PublishedOn = serviceModel.PublishedOn,
            Isbn = string.IsNullOrWhiteSpace(serviceModel.Isbn)
                ? null
                : serviceModel.Isbn.Trim(),
            CreatorId = creatorId,
            IsApproved = false,
            ApprovedOn = null,
            ApprovedBy = null,
            RejectionReason = null,
        };

    public static void UpdateDbModel(
            this CreateBookServiceModel serviceModel,
            BookDbModel dbModel)
    {
        dbModel.Title = serviceModel.Title.Trim();
        dbModel.Author = serviceModel.Author.Trim();
        dbModel.Genre = serviceModel.Genre.Trim();
        dbModel.Description = string.IsNullOrWhiteSpace(serviceModel.Description)
            ? null
            : serviceModel.Description.Trim();
        dbModel.Publisher = string.IsNullOrWhiteSpace(serviceModel.Publisher)
            ? null
            : serviceModel.Publisher.Trim();
        dbModel.PublishedOn = serviceModel.PublishedOn;
        dbModel.Isbn = string.IsNullOrWhiteSpace(serviceModel.Isbn)
            ? null
            : serviceModel.Isbn.Trim();
        dbModel.IsApproved = false;
        dbModel.ApprovedOn = null;
        dbModel.ApprovedBy = null;
        dbModel.RejectionReason = null;
    }

    public static CreateBookServiceModel ToCreateServiceModel(
        this CreateBookWebModel webModel)
        => new()
        {
            Title = webModel.Title,
            Author = webModel.Author,
            Genre = webModel.Genre,
            Description = webModel.Description,
            Publisher = webModel.Publisher,
            PublishedOn = webModel.PublishedOn,
            Isbn = webModel.Isbn,
        };

    public static BookFilterServiceModel ToFilterServiceModel(
        this BookFilterWebModel webModel,
        string? creatorId = null)
        => new()
        {
            SearchTerm = webModel.SearchTerm,
            Title = webModel.Title,
            Author = webModel.Author,
            Genre = webModel.Genre,
            Publisher = webModel.Publisher,
            Isbn = webModel.Isbn,
            CreatorId = creatorId,
            IsApproved = webModel.IsApproved,
            PublishedFrom = webModel.PublishedFrom,
            PublishedTo = webModel.PublishedTo,
            PageIndex = webModel.PageIndex,
            PageSize = webModel.PageSize,
            Sorting = webModel.Sorting,
        };
}
