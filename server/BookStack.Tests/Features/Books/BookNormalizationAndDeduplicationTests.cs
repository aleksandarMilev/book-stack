namespace BookStack.Tests.Features.Books;

using BookStack.Features.Books.Shared;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

public class BookNormalizationAndDeduplicationTests
{
    [Fact]
    public void NormalizeIdentityText_MapsEquivalentAuthorFormsToSameIdentity()
    {
        var withPunctuation = BookMapping.NormalizeIdentityText("J. K. Rowling");
        var compact = BookMapping.NormalizeIdentityText("JK Rowling");
        var dashed = BookMapping.NormalizeIdentityText("Harry-Potter");

        Assert.Equal("JKROWLING", withPunctuation);
        Assert.Equal(withPunctuation, compact);
        Assert.Equal("HARRYPOTTER", dashed);
    }

    [Fact]
    public void NormalizeIdentityIsbn_RemovesSpacesAndHyphens()
    {
        var normalized = BookMapping.NormalizeIdentityIsbn(" 978-0 321-87758-1 ");

        Assert.Equal("9780321877581", normalized);
    }

    [Fact]
    public async Task Create_RejectsDuplicateByNormalizedTitleAndAuthor_WhenIsbnMissing()
    {
        await using var database = new TestDatabaseScope();

        var currentUserService = new TestCurrentUserService
        {
            UserId = "creator-1",
            Username = "creator-1",
        };

        var utc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookService(
            data,
            currentUserService,
            dateTimeProvider);

        var createServiceModel = MarketplaceTestData.CreateBookModel(
            title: "Harry Potter and the Philosopher's Stone",
            author: "J. K. Rowling");

        var firstResult = await service.Create(
            createServiceModel,
            CancellationToken.None);

        var createServiceModelDuplicate = MarketplaceTestData.CreateBookModel(
            title: "Harry-Potter and the Philosopher's Stone",
            author: "JK Rowling");

        var duplicateResult = await service.Create(
            createServiceModelDuplicate,
            CancellationToken.None);

        Assert.True(firstResult.Succeeded);
        Assert.False(duplicateResult.Succeeded);
        Assert.Contains(
            "already exists",
            duplicateResult.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_RejectsDuplicateByNormalizedIsbn_WhenIsbnExists()
    {
        await using var database = new TestDatabaseScope();

        var currentUserService = new TestCurrentUserService
        {
            UserId = "creator-1",
            Username = "creator-1",
        };

        var utc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookService(
            data,
            currentUserService,
            dateTimeProvider);

        var createServiceModel = MarketplaceTestData.CreateBookModel(
            title: "Domain-Driven Design",
            author: "Eric Evans",
            isbn: "978-0-321-12521-7");

        var firstResult = await service.Create(
            createServiceModel,
            CancellationToken.None);

        var createServiceModelDuplicate = MarketplaceTestData.CreateBookModel(
            title: "Completely Different Title",
            author: "Different Author",
            isbn: "978 0 321 12521 7");

        var duplicateResult = await service.Create(
           createServiceModelDuplicate,
           CancellationToken.None);

        Assert.True(firstResult.Succeeded);
        Assert.False(duplicateResult.Succeeded);
        Assert.Contains(
            "already exists",
            duplicateResult.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_AllowsSameTitleAndAuthor_WhenSecondBookHasUniqueIsbn()
    {
        await using var database = new TestDatabaseScope();

        var currentUserService = new TestCurrentUserService
        {
            UserId = "creator-1",
            Username = "creator-1",
        };

        var utc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookService(
            data,
            currentUserService,
            dateTimeProvider);

        var createServiceModel = MarketplaceTestData.CreateBookModel(
            title: "The Pragmatic Programmer",
            author: "Andy Hunt");

        var firstResult = await service.Create(
            createServiceModel,
            CancellationToken.None);

        var createServiceModelDuplicate = MarketplaceTestData.CreateBookModel(
            title: "The Pragmatic Programmer",
            author: "Andy Hunt",
            isbn: "978-0-201-61622-4");

        var secondResult = await service.Create(
            createServiceModelDuplicate,
            CancellationToken.None);

        var booksCount = await data
            .Books
            .CountAsync(CancellationToken.None);

        Assert.True(firstResult.Succeeded);
        Assert.True(secondResult.Succeeded);
        Assert.Equal(2, booksCount);
    }

    [Fact]
    public async Task Edit_RejectsCollisionWithExistingBook_WhenNormalizedIdentityCollides()
    {
        await using var database = new TestDatabaseScope();

        var currentUserService = new TestCurrentUserService
        {
            UserId = "creator-1",
            Username = "creator-1",
        };

        var utc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new TestDateTimeProvider(utc);

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookService(
            data,
            currentUserService,
            dateTimeProvider);

        var firstCreateServiceModel = MarketplaceTestData.CreateBookModel(
            title: "Clean Code",
            author: "Robert C. Martin");

        var firstCreateResult = await service.Create(
            firstCreateServiceModel,
            CancellationToken.None);

        var secondCreateServiceModel = MarketplaceTestData.CreateBookModel(
            title: "Clean Architecture",
            author: "Robert Martin");

        var secondCreateResult = await service.Create(
            secondCreateServiceModel,
            CancellationToken.None);

        var editFirstBookServiceModel = MarketplaceTestData.CreateBookModel(
            title: "Clean-Code",
            author: "Robert C Martin");

        var editResult = await service.Edit(
            secondCreateResult.Data!,
            editFirstBookServiceModel,
            CancellationToken.None);

        Assert.True(firstCreateResult.Succeeded);
        Assert.True(secondCreateResult.Succeeded);
        Assert.False(editResult.Succeeded);
        Assert.Contains(
            "already exists",
            editResult.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }
}
