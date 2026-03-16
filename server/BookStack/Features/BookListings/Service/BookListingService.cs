namespace BookStack.Features.BookListings.Service;

using Books.Data.Models;
using BookStack.Data;
using Common;
using Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.DateTimeProvider;
using Infrastructure.Services.ImageWriter;
using Infrastructure.Services.PageClamper;
using Infrastructure.Services.Result;
using Infrastructure.Services.StringSanitizer;
using Microsoft.EntityFrameworkCore;
using Models;
using Shared;

using static BookStack.Common.Constants;
using static Shared.Constants;

public class BookListingService(
    BookStackDbContext data,
    ICurrentUserService userService,
    IDateTimeProvider dateTimeProvider,
    IPageClamper pageClamper,
    IImageWriter imageWriter,
    IStringSanitizerService stringSanitizer,
    ILogger<BookListingService> logger) : IBookListingService
{
    private readonly BookStackDbContext _data = data;
    private readonly ICurrentUserService _userService = userService;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IPageClamper _pageClamper = pageClamper;
    private readonly IImageWriter _imageWriter = imageWriter;
    private readonly IStringSanitizerService _stringSanitizer = stringSanitizer;
    private readonly ILogger<BookListingService> _logger = logger;

    public async Task<PaginatedModel<BookListingServiceModel>> All(
        BookListingFilterServiceModel filter,
        CancellationToken cancellationToken = default)
    {
        var pageIndex = filter.PageIndex;
        var pageSize = filter.PageSize;

        this._pageClamper.ClampPageSizeAndIndex(
            ref pageIndex,
            ref pageSize,
            Pagination.MaxPageSize);

        var isNotAdmin = !this._userService.IsAdmin();

        var query = ApplyFilter(
            this.AllListingsAsNoTracking(),
            filter,
            forceOnlyApprovedForPublic: isNotAdmin);

        query = ApplySorting(query, filter.Sorting);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToServiceModels()
            .ToListAsync(cancellationToken);

        return new PaginatedModel<BookListingServiceModel>(
            items,
            totalItems,
            pageIndex,
            pageSize);
    }

    public async Task<PaginatedModel<BookListingServiceModel>> Mine(
        BookListingFilterServiceModel filter,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (currentUserId is null)
        {
            return new PaginatedModel<BookListingServiceModel>(
                [],
                0,
                filter.PageIndex,
                filter.PageSize);
        }

        var pageIndex = filter.PageIndex;
        var pageSize = filter.PageSize;

        this._pageClamper.ClampPageSizeAndIndex(
            ref pageIndex,
            ref pageSize,
            Pagination.MaxPageSize);

        filter = new()
        {
            SearchTerm = filter.SearchTerm,
            BookId = filter.BookId,
            Title = filter.Title,
            Author = filter.Author,
            Genre = filter.Genre,
            Publisher = filter.Publisher,
            Isbn = filter.Isbn,
            CreatorId = currentUserId,
            Condition = filter.Condition,
            PriceFrom = filter.PriceFrom,
            PriceTo = filter.PriceTo,
            IsApproved = filter.IsApproved,
            PublishedFrom = filter.PublishedFrom,
            PublishedTo = filter.PublishedTo,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Sorting = filter.Sorting,
        };

        var query = ApplyFilter(
            this.AllListingsAsNoTracking(),
            filter,
            forceOnlyApprovedForPublic: false);

        query = ApplySorting(query, filter.Sorting);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToServiceModels()
            .ToListAsync(cancellationToken);

        return new PaginatedModel<BookListingServiceModel>(
            items,
            totalItems,
            pageIndex,
            pageSize);
    }

    public async Task<BookListingServiceModel?> Details(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        var currentUserIsAdmin = this._userService.IsAdmin();

        var query = this
            .AllListingsAsNoTracking()
            .Where(l => l.Id == id);

        if (!currentUserIsAdmin)
        {
            query = currentUserId is null
                ? query.Where(l => l.IsApproved && l.Book.IsApproved)
                : query.Where(l => (l.IsApproved && l.Book.IsApproved) || l.CreatorId == currentUserId);
        }

        return await query
            .ToServiceModels()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ResultWith<Guid>> Create(
        CreateBookListingServiceModel model,
        CancellationToken cancellationToken = default)
    {
        var creatorId = this._userService.GetId();
        if (string.IsNullOrWhiteSpace(creatorId))
        {
            return ErrorMessages.CurrentUserNotAuthenticated;
        }

        var book = await this._data
            .Books
            .SingleOrDefaultAsync(
                b => b.Id == model.BookId,
                cancellationToken);

        if (book is null)
        {
            return string.Format(
                ErrorMessages.DbEntityNotFound,
                nameof(BookDbModel),
                model.BookId);
        }

        var currentUserIsAdmin = this._userService.IsAdmin();
        var currentUserIsBookCreator = book.CreatorId == creatorId;

        if (!book.IsApproved && !currentUserIsAdmin && !currentUserIsBookCreator)
        {
            return "Book listing can only be created for an approved book.";
        }

        var dbModel = model.ToDbModel(creatorId);

        await this._imageWriter.Write(
            Paths.ListingsImagePathPrefix,
            dbModel,
            model,
            Paths.DefaultImagePath,
            cancellationToken);

        this._data.Add(dbModel);
        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Book listing created. ListingId={ListingId}, BookId={BookId}, CreatorId={CreatorId}",
            dbModel.Id,
            dbModel.BookId,
            creatorId);

        return ResultWith<Guid>.Success(dbModel.Id);
    }

    public async Task<Result> Edit(
        Guid id,
        CreateBookListingServiceModel model,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (currentUserId is null)
        {
            return ErrorMessages.CurrentUserNotAuthenticated;
        }

        var dbModel = await this._data
            .BookListings
            .SingleOrDefaultAsync(
                l => l.Id == id,
                cancellationToken);

        if (DoesNotExistOrDeleted(dbModel))
        {
            return this.LogAndReturnNotFoundMessage(id);
        }

        var currentUserIsAdmin = this._userService.IsAdmin();
        var currentUserIsListingCreator = dbModel!.CreatorId == currentUserId;

        if (!currentUserIsAdmin && !currentUserIsListingCreator)
        {
            return this.LogAndReturnUnauthorizedMessage(currentUserId, id);
        }

        var book = await this._data
            .Books
            .SingleOrDefaultAsync(
                b => b.Id == model.BookId,
                cancellationToken);

        if (book is null)
        {
            return string.Format(
                ErrorMessages.DbEntityNotFound,
                nameof(BookDbModel),
                model.BookId);
        }

        var currentUserIsBookCreator = book.CreatorId == currentUserId;

        if (!book.IsApproved && !currentUserIsAdmin && !currentUserIsBookCreator)
        {
            return "Book listing can only be assigned to an approved book.";
        }

        var oldImagePath = dbModel.ImagePath;

        model.UpdateDbModel(dbModel);

        var willDeleteOld =
            !string.IsNullOrWhiteSpace(oldImagePath) &&
            !string.Equals(
                oldImagePath,
                Paths.DefaultImagePath,
                StringComparison.OrdinalIgnoreCase);

        if (model.RemoveImage)
        {
            dbModel.ImagePath = Paths.DefaultImagePath;
        }
        else
        {
            await this._imageWriter.Write(
                Paths.ListingsImagePathPrefix,
                dbModel,
                model,
                defaultImagePath: null,
                cancellationToken);

            willDeleteOld &= model.Image is not null;
        }

        await this._data.SaveChangesAsync(cancellationToken);

        var shouldDeleteOldImage =
            willDeleteOld &&
            !string.Equals(
                oldImagePath,
                dbModel.ImagePath,
                StringComparison.OrdinalIgnoreCase);

        if (shouldDeleteOldImage)
        {
            var successfullyDeleted = this._imageWriter.Delete(
                Paths.ListingsImagePathPrefix,
                oldImagePath,
                Paths.DefaultImagePath);

            if (!successfullyDeleted)
            {
                this._logger.LogWarning(
                    "Listing updated but old image was not deleted. ListingId={ListingId}, OldImagePath={OldImagePath}, NewImagePath={NewImagePath}",
                    id,
                    oldImagePath,
                    dbModel.ImagePath);
            }
        }

        this._logger.LogInformation(
            "Book listing updated. ListingId={ListingId}, UserId={UserId}",
            id,
            currentUserId);

        return true;
    }

    public async Task<Result> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (currentUserId is null)
        {
            return ErrorMessages.CurrentUserNotAuthenticated;
        }

        var dbModel = await this._data
            .BookListings
            .SingleOrDefaultAsync(
                l => l.Id == id,
                cancellationToken);

        if (DoesNotExistOrDeleted(dbModel))
        {
            return this.LogAndReturnNotFoundMessage(id);
        }

        var userIsNotAdmin = !this._userService.IsAdmin();
        var userIsNotCreator = dbModel!.CreatorId != currentUserId;

        if (userIsNotAdmin && userIsNotCreator)
        {
            return this.LogAndReturnUnauthorizedMessage(currentUserId, id);
        }

        this._data.Remove(dbModel);
        await this._data.SaveChangesAsync(cancellationToken);

        var imageDeleted = this._imageWriter.Delete(
            Paths.ListingsImagePathPrefix,
            dbModel.ImagePath,
            Paths.DefaultImagePath);

        if (!imageDeleted)
        {
            this._logger.LogWarning(
                "Listing deleted but image was not deleted. ListingId={ListingId}, ImagePath={ImagePath}",
                id,
                dbModel.ImagePath);
        }

        this._logger.LogInformation(
            "Book listing deleted. ListingId={ListingId}, UserId={UserId}",
            id,
            currentUserId);

        return true;
    }

    public async Task<Result> Approve(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var listing = await this._data
            .BookListings
            .SingleOrDefaultAsync(
                l => l.Id == id,
                cancellationToken);

        if (DoesNotExistOrDeleted(listing))
        {
            return this.LogAndReturnNotFoundMessage(id);
        }

        var book = await this._data
            .Books
            .SingleOrDefaultAsync(
                b => b.Id == listing!.BookId,
                cancellationToken);

        if (book is null || !book.IsApproved)
        {
            return "Book listing can not be approved because its book is not approved.";
        }

        listing!.IsApproved = true;
        listing.ApprovedOn = this._dateTimeProvider.UtcNow;
        listing.ApprovedBy = this._userService.GetUsername();
        listing.RejectionReason = null;

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Book listing approved. ListingId={ListingId}, ApprovedBy={ApprovedBy}",
            id,
            listing.ApprovedBy);

        return true;
    }

    public async Task<Result> Reject(
        Guid id,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var listing = await this._data
            .BookListings
            .SingleOrDefaultAsync(
                l => l.Id == id,
                cancellationToken);

        if (DoesNotExistOrDeleted(listing))
        {
            return this.LogAndReturnNotFoundMessage(id);
        }

        listing!.IsApproved = false;
        listing.ApprovedOn = null;
        listing.ApprovedBy = null;
        listing.RejectionReason = string.IsNullOrWhiteSpace(rejectionReason)
            ? null
            : rejectionReason.Trim();

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Book listing rejected. ListingId={ListingId}",
            id);

        return true;
    }

    public async Task<IEnumerable<BookListingLookupServiceModel>> Lookup(
        string? query,
        int take = BookStack.Common.Constants.DefaultValues.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(
            take,
            Shared.Constants.Validation.MinLookupSize,
            Shared.Constants.Validation.MaxLookupSize);

        query = query?.Trim();

        var listingsQuery = this._data
            .BookListings
            .AsNoTracking()
            .Where(l => l.IsApproved && l.Book.IsApproved);

        if (!string.IsNullOrWhiteSpace(query))
        {
            listingsQuery = listingsQuery.Where(l =>
                EF.Functions.Like(l.Book.Title, $"%{query}%") ||
                EF.Functions.Like(l.Book.Author, $"%{query}%") ||
                EF.Functions.Like(l.Book.Genre, $"%{query}%") ||
                EF.Functions.Like(l.Description, $"%{query}%") ||
                (l.Book.Isbn != null && EF.Functions.Like(l.Book.Isbn, $"%{query}%")));
        }

        return await listingsQuery
            .OrderBy(l => l.Book.Title)
            .ThenBy(l => l.Price)
            .Take(take)
            .ToLookupServiceModels()
            .ToListAsync(cancellationToken);
    }

    private IQueryable<BookListingDbModel> AllListingsAsNoTracking()
        => this._data
            .BookListings
            .AsNoTracking();

    private static IQueryable<BookListingDbModel> ApplyFilter(
        IQueryable<BookListingDbModel> query,
        BookListingFilterServiceModel filter,
        bool forceOnlyApprovedForPublic)
    {
        if (forceOnlyApprovedForPublic)
        {
            query = query
                .Where(l => l.IsApproved && l.Book.IsApproved);
        }
        else if (filter.IsApproved.HasValue)
        {
            query = query
                .Where(l => l.IsApproved == filter.IsApproved.Value);
        }

        if (filter.BookId.HasValue)
        {
            query = query
                .Where(l => l.BookId == filter.BookId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CreatorId))
        {
            query = query
                .Where(l => l.CreatorId == filter.CreatorId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var normalizedSearchTerm = NormalizeSearchFilter(filter.SearchTerm);
            query = query.Where(l =>
                EF.Functions.Like(l.Book.Title, $"%{normalizedSearchTerm}%") ||
                EF.Functions.Like(l.Book.Author, $"%{normalizedSearchTerm}%") ||
                EF.Functions.Like(l.Book.Genre, $"%{normalizedSearchTerm}%") ||
                EF.Functions.Like(l.Description, $"%{normalizedSearchTerm}%") ||
                (l.Book.Publisher != null && EF.Functions.Like(l.Book.Publisher, $"%{normalizedSearchTerm}%")) ||
                (l.Book.Isbn != null && EF.Functions.Like(l.Book.Isbn, $"%{normalizedSearchTerm}%")) ||
                EF.Functions.Like(l.CreatorId, $"%{normalizedSearchTerm}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var normalizedTitle = NormalizeSearchFilter(filter.Title);
            query = query
                .Where(l => EF.Functions.Like(l.Book.Title, $"%{normalizedTitle}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Author))
        {
            var normalizedAuthor = NormalizeSearchFilter(filter.Author);
            query = query
                .Where(l => EF.Functions.Like(l.Book.Author, $"%{normalizedAuthor}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Genre))
        {
            var normalizedGenre = NormalizeSearchFilter(filter.Genre);
            query = query
                .Where(l => EF.Functions.Like(l.Book.Genre, $"%{normalizedGenre}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Publisher))
        {
            var normalizedPublisher = NormalizeSearchFilter(filter.Publisher);
            query = query
                .Where(l => l.Book.Publisher != null && EF.Functions.Like(l.Book.Publisher, $"%{normalizedPublisher}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Isbn))
        {
            var normalizedIsbn = NormalizeSearchFilter(filter.Isbn);
            query = query
                .Where(l => l.Book.Isbn != null && EF.Functions.Like(l.Book.Isbn, $"%{normalizedIsbn}%"));
        }

        if (filter.Condition.HasValue)
        {
            query = query
                .Where(l => l.Condition == filter.Condition.Value);
        }

        if (filter.PriceFrom.HasValue)
        {
            query = query
                .Where(l => l.Price >= filter.PriceFrom.Value);
        }

        if (filter.PriceTo.HasValue)
        {
            query = query
                .Where(l => l.Price <= filter.PriceTo.Value);
        }

        if (filter.PublishedFrom.HasValue)
        {
            query = query
                .Where(l => l.Book.PublishedOn.HasValue && l.Book.PublishedOn.Value >= filter.PublishedFrom.Value);
        }

        if (filter.PublishedTo.HasValue)
        {
            query = query
                .Where(l => l.Book.PublishedOn.HasValue && l.Book.PublishedOn.Value <= filter.PublishedTo.Value);
        }

        return query;
    }

    private static IQueryable<BookListingDbModel> ApplySorting(
        IQueryable<BookListingDbModel> query,
        ListingSorting sorting)
        => sorting switch
        {
            ListingSorting.Oldest => query
                .OrderBy(static l => l.CreatedOn),
            ListingSorting.PriceAscending => query
                .OrderBy(static l => l.Price)
                .ThenBy(static l => l.CreatedOn),
            ListingSorting.PriceDescending => query
                .OrderByDescending(static l => l.Price)
                .ThenByDescending(static l => l.CreatedOn),
            ListingSorting.TitleAscending => query
                .OrderBy(static l => l.Book.Title)
                .ThenBy(static l => l.Price),
            ListingSorting.TitleDescending => query
                .OrderByDescending(static l => l.Book.Title)
                .ThenByDescending(static l => l.Price),
            ListingSorting.PublishedDateAscending => query
                .OrderByDescending(static l => l.Book.PublishedOn.HasValue)
                .ThenBy(static l => l.Book.PublishedOn)
                .ThenBy(static l => l.Book.Title),
            ListingSorting.PublishedDateDescending => query
                .OrderByDescending(static l => l.Book.PublishedOn.HasValue)
                .ThenByDescending(static l => l.Book.PublishedOn)
                .ThenBy(static l => l.Book.Title),
            _ => query
                .OrderByDescending(static l => l.CreatedOn),
        };

    private static bool DoesNotExistOrDeleted(BookListingDbModel? listing)
        => listing is null || listing.IsDeleted;

    private static string NormalizeSearchFilter(string filter)
        => filter.Trim();

    private string LogAndReturnNotFoundMessage(Guid id)
    {
        this._logger.LogWarning(
            ErrorMessages.DbEntityNotFoundTemplate,
            nameof(BookListingDbModel),
            id);

        return string.Format(
            ErrorMessages.DbEntityNotFound,
            nameof(BookListingDbModel),
            id);
    }

    private string LogAndReturnUnauthorizedMessage(
        string currentUserId,
        Guid listingId)
    {
        var sanitizedCurrentUserId = this._stringSanitizer
            .SanitizeStringForLog(currentUserId);

        this._logger.LogWarning(
            ErrorMessages.UnauthorizedMessageTemplate,
            sanitizedCurrentUserId,
            nameof(BookListingDbModel),
            listingId);

        return string.Format(
            ErrorMessages.UnauthorizedMessage,
            sanitizedCurrentUserId,
            nameof(BookListingDbModel),
            listingId);
    }
}
