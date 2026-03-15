namespace BookStack.Features.Books.Service;

using Common;
using Infrastructure.Services.Result;
using Infrastructure.Services.ServiceLifetimes;
using Models;

using static Common.Constants;

public interface IBookService : IScopedService
{
    Task<PaginatedModel<BookServiceModel>> All(
        BookFilterServiceModel filter,
        CancellationToken cancellationToken = default);

    Task<PaginatedModel<BookServiceModel>> Mine(
        BookFilterServiceModel filter,
        CancellationToken cancellationToken = default);

    Task<BookServiceModel?> Details(
        Guid bookId,
        CancellationToken cancellationToken = default);

    Task<ResultWith<Guid>> Create(
        CreateBookServiceModel model,
        CancellationToken cancellationToken = default);

    Task<Result> Edit(
        Guid bookId,
        CreateBookServiceModel model,
        CancellationToken cancellationToken = default);

    Task<Result> Delete(
        Guid bookId,
        CancellationToken cancellationToken = default);

    Task<Result> Approve(
        Guid bookId,
        CancellationToken cancellationToken = default);

    Task<Result> Reject(
        Guid bookId,
        string? rejectionReason,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BookLookupServiceModel>> Lookup(
        string? query,
        int take = DefaultValues.DefaultPageSize,
        CancellationToken cancellationToken = default);
}