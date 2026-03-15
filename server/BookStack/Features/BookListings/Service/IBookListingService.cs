namespace BookStack.Features.BookListings.Service;

using Common;
using Infrastructure.Services.Result;
using Infrastructure.Services.ServiceLifetimes;
using Models;

public interface IBookListingService : IScopedService
{
    Task<PaginatedModel<BookListingServiceModel>> All(
        BookListingFilterServiceModel filter,
        CancellationToken cancellationToken = default);

    Task<PaginatedModel<BookListingServiceModel>> Mine(
        BookListingFilterServiceModel filter,
        CancellationToken cancellationToken = default);

    Task<BookListingServiceModel?> Details(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ResultWith<Guid>> Create(
        CreateBookListingServiceModel model,
        CancellationToken cancellationToken = default);

    Task<Result> Edit(
        Guid id,
        CreateBookListingServiceModel model,
        CancellationToken cancellationToken = default);

    Task<Result> Delete(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> Approve(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> Reject(
        Guid id,
        string? rejectionReason,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BookListingLookupServiceModel>> Lookup(
        string? query,
        int take = Constants.DefaultValues.DefaultPageSize,
        CancellationToken cancellationToken = default);
}
