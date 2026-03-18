namespace BookStack.Tests.Features.Books;

using BookStack.Common;
using BookStack.Features.Books.Service;
using BookStack.Features.Books.Service.Models;
using BookStack.Features.Books.Web.Models;
using BookStack.Features.Books.Web.User;
using BookStack.Features.SellerProfiles.Service;
using BookStack.Features.SellerProfiles.Service.Models;
using BookStack.Infrastructure.Services.Result;
using BookStack.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class BooksVisibilityBoundaryTests
{
    [Fact]
    public void CanonicalBrowseEndpoints_AreNotAllowAnonymous()
    {
        AssertActionHasNoAllowAnonymous(nameof(BooksController.All));
        AssertActionHasNoAllowAnonymous(nameof(BooksController.Details));
        AssertActionHasNoAllowAnonymous(nameof(BooksController.Lookup));
    }

    [Fact]
    public async Task All_ForAuthenticatedNonSeller_ReturnsForbid()
    {
        var controller = CreateController(
            currentUserService: new TestCurrentUserService
            {
                UserId = "buyer-1",
                Username = "buyer-1",
                Admin = false,
            },
            hasActiveSellerProfile: false);

        var response = await controller.All(
            new BookFilterWebModel(),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(response.Result);
    }

    [Fact]
    public async Task Lookup_ForActiveSeller_ForwardsRequest()
    {
        var service = new CapturingBookService();
        var controller = CreateController(
            service,
            new TestCurrentUserService
            {
                UserId = "seller-1",
                Username = "seller-1",
                Admin = false,
            },
            hasActiveSellerProfile: true);

        var response = await controller.Lookup(
            "domain",
            5,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var models = Assert.IsAssignableFrom<IEnumerable<BookLookupServiceModel>>(okResult.Value);
        var model = Assert.Single(models);
        Assert.Equal("Domain-Driven Design", model.Title);
        Assert.Equal("domain", service.LookupQuery);
        Assert.Equal(5, service.LookupTake);
    }

    [Fact]
    public async Task All_ForAdmin_DoesNotRequireSellerProfile()
    {
        var service = new CapturingBookService();
        var controller = CreateController(
            service,
            new TestCurrentUserService
            {
                UserId = "admin-1",
                Username = "admin-1",
                Admin = true,
            },
            hasActiveSellerProfile: false);

        var response = await controller.All(
            new BookFilterWebModel(),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var model = Assert.IsType<PaginatedModel<BookServiceModel>>(okResult.Value);
        Assert.Single(model.Items);
        Assert.Equal("Canonical Book", model.Items.Single().Title);
    }

    private static void AssertActionHasNoAllowAnonymous(string actionName)
    {
        var action = typeof(BooksController)
            .GetMethods()
            .Single(method => method.Name == actionName);

        var allowAnonymous = action
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .SingleOrDefault();

        Assert.Null(allowAnonymous);
    }

    private static BooksController CreateController(
        TestCurrentUserService currentUserService,
        bool hasActiveSellerProfile)
        => CreateController(
            new CapturingBookService(),
            currentUserService,
            hasActiveSellerProfile);

    private static BooksController CreateController(
        CapturingBookService service,
        TestCurrentUserService currentUserService,
        bool hasActiveSellerProfile)
        => new(
            service,
            currentUserService,
            new StubSellerProfileService(hasActiveSellerProfile));

    private sealed class CapturingBookService : IBookService
    {
        public string? LookupQuery { get; private set; }

        public int? LookupTake { get; private set; }

        public Task<PaginatedModel<BookServiceModel>> All(
            BookFilterServiceModel filter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PaginatedModel<BookServiceModel>(
                [
                    new BookServiceModel
                    {
                        Id = Guid.NewGuid(),
                        Title = "Canonical Book",
                        Author = "Author",
                        Genre = "Genre",
                        CreatorId = "seller-1",
                        IsApproved = true,
                        CreatedOn = DateTime.UtcNow.ToString("O"),
                    },
                ],
                1,
                filter.PageIndex,
                filter.PageSize));

        public Task<PaginatedModel<BookServiceModel>> Mine(
            BookFilterServiceModel filter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PaginatedModel<BookServiceModel>([], 0, filter.PageIndex, filter.PageSize));

        public Task<BookServiceModel?> Details(
            Guid bookId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<BookServiceModel?>(null);

        public Task<ResultWith<Guid>> Create(
            CreateBookServiceModel model,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ResultWith<Guid>.Success(Guid.NewGuid()));

        public Task<Result> Edit(
            Guid bookId,
            CreateBookServiceModel model,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new Result(true));

        public Task<Result> Delete(
            Guid bookId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new Result(true));

        public Task<Result> Approve(
            Guid bookId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new Result(true));

        public Task<Result> Reject(
            Guid bookId,
            string? rejectionReason,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new Result(true));

        public Task<IEnumerable<BookLookupServiceModel>> Lookup(
            string? query,
            int take = Constants.DefaultValues.DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            this.LookupQuery = query;
            this.LookupTake = take;

            return Task.FromResult<IEnumerable<BookLookupServiceModel>>(
                [
                    new BookLookupServiceModel
                    {
                        Id = Guid.NewGuid(),
                        Title = "Domain-Driven Design",
                        Author = "Eric Evans",
                        Genre = "Software",
                    },
                ]);
        }
    }

    private sealed class StubSellerProfileService(
        bool hasActiveSellerProfile) : ISellerProfileService
    {
        public Task<bool> HasActiveProfile(
            string userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(hasActiveSellerProfile);

        public Task<IEnumerable<SellerProfileServiceModel>> All(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SellerProfileServiceModel?> ByUserId(
            string userId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SellerProfileServiceModel?> Mine(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ResultWith<SellerProfileServiceModel>> UpsertMine(
            UpsertSellerProfileServiceModel model,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result> ChangeStatus(
            string userId,
            bool isActive,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SellerProfileServiceModel?> ActiveByUserId(
            string userId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
