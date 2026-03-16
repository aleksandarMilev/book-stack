namespace BookStack.Features.Books.Service;

using BookStack.Data;
using Common;
using Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.DateTimeProvider;
using Infrastructure.Services.PageClamper;
using Infrastructure.Services.Result;
using Infrastructure.Services.StringSanitizer;
using Microsoft.EntityFrameworkCore;
using Models;
using Shared;

using static Common.Constants;
using static Shared.Constants;

public class BookService(
    BookStackDbContext data,
    ICurrentUserService userService,
    IPageClamper pageClamper,
    IStringSanitizerService stringSanitizer,
    IDateTimeProvider dateTimeProvider,
    ILogger<BookService> logger) : IBookService
{
    private readonly BookStackDbContext _data = data;
    private readonly ICurrentUserService _userService = userService;
    private readonly IPageClamper _pageClamper = pageClamper;
    private readonly IStringSanitizerService _stringSanitizer = stringSanitizer;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly ILogger<BookService> _logger = logger;

    public async Task<PaginatedModel<BookServiceModel>> All(
        BookFilterServiceModel filter,
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
            this.AllBooksAsNoTracking(),
            filter,
            forceOnlyApprovedForPublic: isNotAdmin);

        query = ApplySorting(query, filter.Sorting);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToServiceModels()
            .ToListAsync(cancellationToken);

        return new PaginatedModel<BookServiceModel>(
            items,
            totalItems,
            pageIndex,
            pageSize);
    }

    public async Task<PaginatedModel<BookServiceModel>> Mine(
        BookFilterServiceModel filter,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (currentUserId is null)
        {
            return new PaginatedModel<BookServiceModel>(
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
            Title = filter.Title,
            Author = filter.Author,
            Genre = filter.Genre,
            Publisher = filter.Publisher,
            Isbn = filter.Isbn,
            IsApproved = filter.IsApproved,
            PublishedFrom = filter.PublishedFrom,
            PublishedTo = filter.PublishedTo,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Sorting = filter.Sorting,
            CreatorId = currentUserId,
        };

        var query = ApplyFilter(
            this.AllBooksAsNoTracking(),
            filter,
            forceOnlyApprovedForPublic: false);

        query = ApplySorting(query, filter.Sorting);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToServiceModels()
            .ToListAsync(cancellationToken);

        return new PaginatedModel<BookServiceModel>(
            items,
            totalItems,
            pageIndex,
            pageSize);
    }

    public async Task<BookServiceModel?> Details(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        var currentUserIsAdmin = this._userService.IsAdmin();

        var query = this
            .AllBooksAsNoTracking()
            .Where(b => b.Id == bookId);

        if (!currentUserIsAdmin)
        {
            query = currentUserId is null
                ? query.Where(b => b.IsApproved)
                : query.Where(b => b.IsApproved || b.CreatorId == currentUserId);
        }

        return await query
            .ToServiceModels()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ResultWith<Guid>> Create(
        CreateBookServiceModel model,
        CancellationToken cancellationToken = default)
    {
        var creatorId = this._userService.GetId();
        if (string.IsNullOrWhiteSpace(creatorId))
        {
            return ErrorMessages.CurrentUserNotAuthenticated;
        }

        var title = model.Title.Trim();
        var author = model.Author.Trim();
        var isbn = string.IsNullOrWhiteSpace(model.Isbn)
            ? null
            : model.Isbn.Trim();
        var normalizedTitle = BookMapping.NormalizeIdentityText(model.Title);
        var normalizedAuthor = BookMapping.NormalizeIdentityText(model.Author);
        var normalizedIsbn = BookMapping.NormalizeIdentityIsbn(model.Isbn);

        var existingQuery = this._data
            .Books
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedIsbn))
        {
            var isbnExists = await existingQuery
                .AnyAsync(
                    b => b.NormalizedIsbn == normalizedIsbn,
                    cancellationToken);

            if (isbnExists)
            {
                return $"Book with ISBN '{isbn}' already exists.";
            }
        }
        else
        {
            var titleAuthorAndAuthorExists = await existingQuery
                .AnyAsync(
                    b => b.NormalizedTitle == normalizedTitle && b.NormalizedAuthor == normalizedAuthor,
                    cancellationToken);

            if (titleAuthorAndAuthorExists)
            {
                return $"Book '{title}' by '{author}' already exists.";
            }
        }

        var dbModel = model.ToDbModel(creatorId);

        this._data.Add(dbModel);
        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Book created. BookId={BookId}, CreatorId={CreatorId}, Approved={Approved}",
            dbModel.Id,
            creatorId,
            dbModel.IsApproved);

        return ResultWith<Guid>.Success(dbModel.Id);
    }

    public async Task<Result> Edit(
        Guid bookId,
        CreateBookServiceModel model,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (currentUserId is null)
        {
            return ErrorMessages.CurrentUserNotAuthenticated;
        }

        var dbModel = await this._data
            .Books
            .SingleOrDefaultAsync(
                b => b.Id == bookId,
                cancellationToken);

        if (DoesNotExistOrDeleted(dbModel))
        {
            return this.LogAndReturnNotFoundMessage(bookId);
        }

        var userIsNotAdmin = !this._userService.IsAdmin();
        var userIsNotCreator = dbModel!.CreatorId != currentUserId;

        if (userIsNotAdmin && userIsNotCreator)
        {
            return this.LogAndReturnUnauthorizedMessage(
                currentUserId,
                bookId);
        }

        var title = model.Title.Trim();
        var author = model.Author.Trim();
        var isbn = string.IsNullOrWhiteSpace(model.Isbn)
            ? null
            : model.Isbn.Trim();
        var normalizedTitle = BookMapping.NormalizeIdentityText(model.Title);
        var normalizedAuthor = BookMapping.NormalizeIdentityText(model.Author);
        var normalizedIsbn = BookMapping.NormalizeIdentityIsbn(model.Isbn);

        var existingQuery = this._data
            .Books
            .Where(b => b.Id != bookId);

        if (!string.IsNullOrWhiteSpace(normalizedIsbn))
        {
            var isbnExists = await existingQuery
                .AnyAsync(
                    b => b.NormalizedIsbn == normalizedIsbn,
                    cancellationToken);

            if (isbnExists)
            {
                return $"Book with ISBN '{isbn}' already exists.";
            }
        }
        else
        {
            var titleAndAuthorExists = await existingQuery
                .AnyAsync(
                    b => b.NormalizedTitle == normalizedTitle && b.NormalizedAuthor == normalizedAuthor,
                    cancellationToken);

            if (titleAndAuthorExists)
            {
                return $"Book '{title}' by '{author}' already exists.";
            }
        }

        model.UpdateDbModel(dbModel);
        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Book updated. BookId={BookId}, UserId={UserId}, ApprovedReset={ApprovedReset}",
            bookId,
            currentUserId,
            !dbModel.IsApproved);

        return true;
    }

    public async Task<Result> Delete(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (currentUserId is null)
        {
            return ErrorMessages.CurrentUserNotAuthenticated;
        }

        var dbModel = await this._data
            .Books
            .SingleOrDefaultAsync(
                b => b.Id == bookId,
                cancellationToken);

        if (DoesNotExistOrDeleted(dbModel))
        {
            return this.LogAndReturnNotFoundMessage(bookId);
        }

        var userIsNotAdmin = !this._userService.IsAdmin();
        var userIsNotCreator = dbModel!.CreatorId != currentUserId;

        if (userIsNotAdmin && userIsNotCreator)
        {
            return this.LogAndReturnUnauthorizedMessage(
                currentUserId,
                bookId);
        }

        var hasListings = await this._data
            .BookListings
            .IgnoreQueryFilters()
            .AnyAsync(
                l => l.BookId == bookId,
                cancellationToken);

        if (hasListings)
        {
            return "Book can not be deleted because it has related listings.";
        }

        this._data.Remove(dbModel);
        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Book deleted. BookId={BookId}, UserId={UserId}",
            bookId,
            currentUserId);

        return true;
    }

    public async Task<Result> Approve(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var book = await this._data
            .Books
            .SingleOrDefaultAsync(
                b => b.Id == bookId,
                cancellationToken);

        if (DoesNotExistOrDeleted(book))
        {
            return this.LogAndReturnNotFoundMessage(bookId);
        }

        book!.IsApproved = true;
        book.ApprovedOn = this._dateTimeProvider.UtcNow;
        book.ApprovedBy = this._userService.GetUsername();
        book.RejectionReason = null;

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Book approved. BookId={BookId}, ApprovedBy={ApprovedBy}",
            bookId,
            book.ApprovedBy);

        return true;
    }

    public async Task<Result> Reject(
        Guid bookId,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var book = await this._data
            .Books
            .SingleOrDefaultAsync(
                b => b.Id == bookId,
                cancellationToken);

        if (DoesNotExistOrDeleted(book))
        {
            return this.LogAndReturnNotFoundMessage(bookId);
        }

        book!.IsApproved = false;
        book.ApprovedOn = null;
        book.ApprovedBy = null;
        book.RejectionReason = string.IsNullOrWhiteSpace(rejectionReason)
            ? null
            : rejectionReason.Trim();

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Book rejected. BookId={BookId}",
            bookId);

        return true;
    }

    public async Task<IEnumerable<BookLookupServiceModel>> Lookup(
        string? query,
        int take = DefaultValues.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(
            take,
            Shared.Constants.Validation.MinLookupSize,
            Shared.Constants.Validation.MaxLookupSize);

        query = query?.Trim();

        var booksQuery = this._data
            .Books
            .AsNoTracking()
            .Where(b => b.IsApproved);

        if (!string.IsNullOrWhiteSpace(query))
        {
            booksQuery = booksQuery.Where(b =>
                EF.Functions.Like(b.Title, $"%{query}%") ||
                EF.Functions.Like(b.Author, $"%{query}%") ||
                EF.Functions.Like(b.Genre, $"%{query}%") ||
                (b.Publisher != null && EF.Functions.Like(b.Publisher, $"%{query}%")) ||
                (b.Isbn != null && EF.Functions.Like(b.Isbn, $"%{query}%")));
        }

        return await booksQuery
            .OrderBy(b => b.Title)
            .ThenBy(b => b.Author)
            .Take(take)
            .ToLookupServiceModels()
            .ToListAsync(cancellationToken);
    }

    private IQueryable<BookDbModel> AllBooksAsNoTracking()
        => this.
            _data
            .Books
            .AsNoTracking();

    private static IQueryable<BookDbModel> ApplyFilter(
        IQueryable<BookDbModel> query,
        BookFilterServiceModel filter,
        bool forceOnlyApprovedForPublic)
    {
        if (forceOnlyApprovedForPublic)
        {
            query = query
                .Where(b => b.IsApproved);
        }
        else if (filter.IsApproved.HasValue)
        {
            query = query
                .Where(b => b.IsApproved == filter.IsApproved.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CreatorId))
        {
            query = query
                .Where(b => b.CreatorId == filter.CreatorId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var normalizedSearchTerm = NormalizeSearchFilter(filter.SearchTerm);
            query = query.Where(b =>
                EF.Functions.Like(b.Title, $"%{normalizedSearchTerm}%") ||
                EF.Functions.Like(b.Author, $"%{normalizedSearchTerm}%") ||
                EF.Functions.Like(b.Genre, $"%{normalizedSearchTerm}%") ||
                (b.Publisher != null && EF.Functions.Like(b.Publisher, $"%{normalizedSearchTerm}%")) ||
                (b.Isbn != null && EF.Functions.Like(b.Isbn, $"%{normalizedSearchTerm}%")));
        }

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var normalizedTitle = NormalizeSearchFilter(filter.Title);
            query = query
                .Where(b => EF.Functions.Like(b.Title, $"%{normalizedTitle}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Author))
        {
            var normalizedAuthor = NormalizeSearchFilter(filter.Author);
            query = query
                .Where(b => EF.Functions.Like(b.Author, $"%{normalizedAuthor}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Genre))
        {
            var normalizedGenre = NormalizeSearchFilter(filter.Genre);
            query = query
                .Where(b => EF.Functions.Like(b.Genre, $"%{normalizedGenre}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Publisher))
        {
            var normalizedPublisher = NormalizeSearchFilter(filter.Publisher);
            query = query
                .Where(b =>
                    b.Publisher != null &&
                    EF.Functions.Like(b.Publisher, $"%{normalizedPublisher}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Isbn))
        {
            var normalizedIsbn = NormalizeSearchFilter(filter.Isbn);
            query = query
                .Where(b =>
                    b.Isbn != null &&
                    EF.Functions.Like(b.Isbn, $"%{normalizedIsbn}%"));
        }

        if (filter.PublishedFrom.HasValue)
        {
            query = query
                .Where(b =>
                    b.PublishedOn.HasValue && 
                    b.PublishedOn.Value >= filter.PublishedFrom.Value);
        }

        if (filter.PublishedTo.HasValue)
        {
            query = query
                .Where(b =>
                    b.PublishedOn.HasValue && 
                    b.PublishedOn.Value <= filter.PublishedTo.Value);
        }

        return query;
    }

    private static bool DoesNotExistOrDeleted(BookDbModel? book)
        => book is null || book.IsDeleted;

    private static IQueryable<BookDbModel> ApplySorting(
        IQueryable<BookDbModel> query,
        BookSorting sorting)
        => sorting switch
        {
            BookSorting.Oldest => query
                .OrderBy(static b => b.CreatedOn),
            BookSorting.TitleAscending => query
                .OrderBy(static b => b.Title)
                .ThenBy(static b => b.Author),
            BookSorting.TitleDescending => query
                .OrderByDescending(static b => b.Title)
                .ThenByDescending(static b => b.Author),
            BookSorting.PublishedDateDescending => query
                .OrderByDescending(static b => b.PublishedOn.HasValue)
                .ThenByDescending(static b => b.PublishedOn)
                .ThenBy(static b => b.Title),
            BookSorting.PublishedDateAscending => query
                .OrderByDescending(static b => b.PublishedOn.HasValue)
                .ThenBy(static b => b.PublishedOn)
                .ThenBy(static b => b.Title),
            _ => query
                .OrderByDescending(static b => b.CreatedOn),
        };

    private static string NormalizeSearchFilter(string filter)
        => filter.Trim();

    private string LogAndReturnNotFoundMessage(Guid bookId)
    {
        this._logger.LogWarning(
            ErrorMessages.DbEntityNotFoundTemplate,
            nameof(BookDbModel),
            bookId);

        return string.Format(
            ErrorMessages.DbEntityNotFound,
            nameof(BookDbModel),
            bookId);
    }

    private string LogAndReturnUnauthorizedMessage(
        string currentUserId,
        Guid bookId)
    {
        var sanitizedCurrentUserId = this._stringSanitizer
            .SanitizeStringForLog(currentUserId);

        this._logger.LogWarning(
            ErrorMessages.UnauthorizedMessageTemplate,
            sanitizedCurrentUserId,
            nameof(BookDbModel),
            bookId);

        return string.Format(
            ErrorMessages.UnauthorizedMessage,
            sanitizedCurrentUserId,
            nameof(BookDbModel),
            bookId);
    }
}
