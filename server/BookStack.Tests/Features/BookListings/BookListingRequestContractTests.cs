namespace BookStack.Tests.Features.BookListings;

using BookStack.Common;
using BookStack.Features.BookListings.Service;
using BookStack.Features.BookListings.Service.Models;
using BookStack.Features.BookListings.Shared;
using BookStack.Features.BookListings.Web.Models;
using BookStack.Features.BookListings.Web.User;
using BookStack.Infrastructure.Services.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class BookListingRequestContractTests
{
    [Fact]
    public void CreateActions_UseFromFormBindingForListingModels()
    {
        AssertMethodUsesFromForm(
            typeof(BookListingsController),
            nameof(BookListingsController.Create),
            typeof(CreateBookListingWebModel));

        AssertMethodUsesFromForm(
            typeof(BookListingsController),
            nameof(BookListingsController.CreateWithBook),
            typeof(CreateBookListingWithBookWebModel));

        AssertMethodUsesFromForm(
            typeof(BookListingsController),
            nameof(BookListingsController.Edit),
            typeof(CreateBookListingWebModel));
    }

    [Fact]
    public async Task Create_ForwardsExpectedPayloadWithoutImage()
    {
        var listingService = new CapturingBookListingService(
            createResult: ResultWith<Guid>.Success(Guid.NewGuid()),
            createWithBookResult: ResultWith<Guid>.Success(Guid.NewGuid()),
            editResult: new Result(true));

        var controller = new BookListingsController(listingService);

        var webModel = new CreateBookListingWebModel
        {
            BookId = Guid.NewGuid(),
            Price = 19.99m,
            Currency = "EUR",
            Condition = ListingCondition.VeryGood,
            Quantity = 2,
            Description = "Very good listing description.",
            Image = null,
            RemoveImage = false
        };

        var response = await controller.Create(
            webModel,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(listingService.CreateResult.Data, Assert.IsType<Guid>(okResult.Value));
        Assert.NotNull(listingService.CreateInput);
        Assert.Equal(webModel.BookId, listingService.CreateInput!.BookId);
        Assert.Equal(webModel.Price, listingService.CreateInput.Price);
        Assert.Equal(webModel.Currency, listingService.CreateInput.Currency);
        Assert.Equal(webModel.Condition, listingService.CreateInput.Condition);
        Assert.Equal(webModel.Quantity, listingService.CreateInput.Quantity);
        Assert.Equal(webModel.Description, listingService.CreateInput.Description);
        Assert.False(listingService.CreateInput.RemoveImage);
        Assert.Null(listingService.CreateInput.Image);
    }

    [Fact]
    public async Task CreateWithBook_ForwardsExpectedPayloadWithImage()
    {
        var listingService = new CapturingBookListingService(
            createResult: ResultWith<Guid>.Success(Guid.NewGuid()),
            createWithBookResult: ResultWith<Guid>.Success(Guid.NewGuid()),
            editResult: new Result(true));

        var controller = new BookListingsController(listingService);
        var image = CreateFormFile("listing.png", "image/png", "listing-bytes");

        var webModel = new CreateBookListingWithBookWebModel
        {
            Title = "Clean Architecture",
            Author = "Robert C. Martin",
            Genre = "Software",
            BookDescription = "Architecture fundamentals.",
            Publisher = "Prentice Hall",
            PublishedOn = new DateOnly(2017, 9, 20),
            Isbn = "9780134494166",
            Price = 24.50m,
            Currency = "EUR",
            Condition = ListingCondition.Good,
            Quantity = 1,
            Description = "Used but in great condition.",
            Image = image
        };

        var response = await controller.CreateWithBook(
            webModel,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(listingService.CreateWithBookResult.Data, Assert.IsType<Guid>(okResult.Value));
        Assert.NotNull(listingService.CreateWithBookInput);
        Assert.Equal(webModel.Title, listingService.CreateWithBookInput!.Book.Title);
        Assert.Equal(webModel.Author, listingService.CreateWithBookInput.Book.Author);
        Assert.Equal(webModel.Genre, listingService.CreateWithBookInput.Book.Genre);
        Assert.Equal(webModel.BookDescription, listingService.CreateWithBookInput.Book.Description);
        Assert.Equal(webModel.Publisher, listingService.CreateWithBookInput.Book.Publisher);
        Assert.Equal(webModel.PublishedOn, listingService.CreateWithBookInput.Book.PublishedOn);
        Assert.Equal(webModel.Isbn, listingService.CreateWithBookInput.Book.Isbn);
        Assert.Equal(webModel.Price, listingService.CreateWithBookInput.Price);
        Assert.Equal(webModel.Currency, listingService.CreateWithBookInput.Currency);
        Assert.Equal(webModel.Condition, listingService.CreateWithBookInput.Condition);
        Assert.Equal(webModel.Quantity, listingService.CreateWithBookInput.Quantity);
        Assert.Equal(webModel.Description, listingService.CreateWithBookInput.Description);
        Assert.Same(image, listingService.CreateWithBookInput.Image);
        Assert.False(listingService.CreateWithBookInput.RemoveImage);
    }

    [Fact]
    public async Task Edit_ForwardsRemoveImageFlag()
    {
        var listingService = new CapturingBookListingService(
            createResult: ResultWith<Guid>.Success(Guid.NewGuid()),
            createWithBookResult: ResultWith<Guid>.Success(Guid.NewGuid()),
            editResult: new Result(true));

        var controller = new BookListingsController(listingService);
        var listingId = Guid.NewGuid();
        var webModel = new CreateBookListingWebModel
        {
            BookId = Guid.NewGuid(),
            Price = 12.30m,
            Currency = "EUR",
            Condition = ListingCondition.Acceptable,
            Quantity = 3,
            Description = "Editable listing description.",
            Image = null,
            RemoveImage = true
        };

        var response = await controller.Edit(
            listingId,
            webModel,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        Assert.Equal(listingId, listingService.EditId);
        Assert.NotNull(listingService.EditInput);
        Assert.True(listingService.EditInput!.RemoveImage);
        Assert.Null(listingService.EditInput.Image);
    }

    private static void AssertMethodUsesFromForm(
        Type controllerType,
        string methodName,
        Type modelType)
    {
        var method = controllerType.GetMethods()
            .Single(m => m.Name == methodName && m.GetParameters().Any(p => p.ParameterType == modelType));

        var modelParameter = method.GetParameters()
            .Single(parameter => parameter.ParameterType == modelType);

        var fromFormAttribute = modelParameter
            .GetCustomAttributes(typeof(FromFormAttribute), inherit: false)
            .SingleOrDefault();

        Assert.NotNull(fromFormAttribute);
    }

    private static IFormFile CreateFormFile(
        string fileName,
        string contentType,
        string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "image", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class CapturingBookListingService(
        ResultWith<Guid> createResult,
        ResultWith<Guid> createWithBookResult,
        Result editResult) : IBookListingService
    {
        public ResultWith<Guid> CreateResult { get; } = createResult;

        public ResultWith<Guid> CreateWithBookResult { get; } = createWithBookResult;

        public Guid? EditId { get; private set; }

        public CreateBookListingServiceModel? CreateInput { get; private set; }

        public CreateBookListingWithBookServiceModel? CreateWithBookInput { get; private set; }

        public CreateBookListingServiceModel? EditInput { get; private set; }

        public Task<ResultWith<Guid>> Create(
            CreateBookListingServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.CreateInput = model;
            return Task.FromResult(this.CreateResult);
        }

        public Task<ResultWith<Guid>> CreateWithBook(
            CreateBookListingWithBookServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.CreateWithBookInput = model;
            return Task.FromResult(this.CreateWithBookResult);
        }

        public Task<Result> Edit(
            Guid id,
            CreateBookListingServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.EditId = id;
            this.EditInput = model;
            return Task.FromResult(editResult);
        }

        public Task<PaginatedModel<BookListingServiceModel>> All(
            BookListingFilterServiceModel filter,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PaginatedModel<BookListingServiceModel>> Mine(
            BookListingFilterServiceModel filter,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<BookListingServiceModel?> Details(
            Guid id,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result> Delete(
            Guid id,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result> Approve(
            Guid id,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result> Reject(
            Guid id,
            string? rejectionReason,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IEnumerable<BookListingLookupServiceModel>> Lookup(
            string? query,
            int take = Common.Constants.DefaultValues.DefaultPageSize,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
