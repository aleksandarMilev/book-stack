namespace BookStack.Data.Seeders.Books;

using Areas.Admin.Service;
using BookStack.Infrastructure.Services.DateTimeProvider;
using Features.Books.Data.Models;
using Microsoft.EntityFrameworkCore;

using static Common.Constants;

public sealed class BookSeeder(
    BookStackDbContext data,
    IAdminService adminService,
    IDateTimeProvider dateTimeProvider,
    ILogger<BookSeeder> logger) : IBookSeeder
{
    private const string ZeroOrMoreThanOneUsersErrorMessage =
        "There should be exactly one non-admin user in dev environment so books seeding works correctly.";

    public async Task Seed(CancellationToken cancellationToken)
    {
        string? userId;

        try
        {
            userId = await data
                .Users
                .AsNoTracking()
                .Where(static u => u.UserName != Names.AdminRoleName)
                .Select(static u => u.Id)
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning(ZeroOrMoreThanOneUsersErrorMessage);
            throw new InvalidOperationException(ZeroOrMoreThanOneUsersErrorMessage);
        }

        if (userId is null)
        {
            logger.LogWarning(ZeroOrMoreThanOneUsersErrorMessage);
            throw new InvalidOperationException(ZeroOrMoreThanOneUsersErrorMessage);
        }

        var adminId = await adminService.GetId();

        var hasBooks = await data
            .Books
            .AsNoTracking()
            .AnyAsync(cancellationToken);

        if (hasBooks)
        {
            logger.LogInformation("Books already exist. Skipping books seed.");
            return;
        }

        var approvedOn = dateTimeProvider.UtcNow;

        var books = new List<BookDbModel>
        {
            CreateBook(
                "The Great Gatsby",
                "F. Scott Fitzgerald",
                "Classic",
                "A portrait of wealth, longing, and illusion in the Jazz Age.",
                "Charles Scribner's Sons",
                new DateOnly(1925, 4, 10),
                "9780743273565",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "To Kill a Mockingbird",
                "Harper Lee",
                "Classic",
                "A novel of justice, conscience, and childhood in the American South.",
                "J. B. Lippincott & Co.",
                new DateOnly(1960, 7, 11),
                "9780061120084",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "1984",
                "George Orwell",
                "Dystopian",
                "A bleak vision of surveillance, propaganda, and authoritarian control.",
                "Secker & Warburg",
                new DateOnly(1949, 6, 8),
                "9780451524935",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "Pride and Prejudice",
                "Jane Austen",
                "Romance",
                "A sharp and enduring novel about manners, marriage, and character.",
                "T. Egerton",
                new DateOnly(1813, 1, 28),
                "9780141439518",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "The Catcher in the Rye",
                "J. D. Salinger",
                "Coming-of-Age",
                "A teenage voice wrestling with alienation, grief, and identity.",
                "Little, Brown and Company",
                new DateOnly(1951, 7, 16),
                "9780316769488",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "The Hobbit",
                "J. R. R. Tolkien",
                "Fantasy",
                "Bilbo Baggins is swept into an adventure of dragons, treasure, and courage.",
                "George Allen & Unwin",
                new DateOnly(1937, 9, 21),
                "9780547928227",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "The Lord of the Rings",
                "J. R. R. Tolkien",
                "Fantasy",
                "An epic quest to destroy the One Ring and defeat the shadow of Sauron.",
                "George Allen & Unwin",
                new DateOnly(1954, 7, 29),
                "9780618640157",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "Harry Potter and the Philosopher's Stone",
                "J. K. Rowling",
                "Fantasy",
                "A young boy discovers he is a wizard and enters a hidden magical world.",
                "Bloomsbury",
                new DateOnly(1997, 6, 26),
                "9780747532699",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "The Alchemist",
                "Paulo Coelho",
                "Adventure",
                "A philosophical fable about destiny, risk, and personal legend.",
                "HarperTorch",
                new DateOnly(1988, 1, 1),
                "9780061122415",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "The Little Prince",
                "Antoine de Saint-Exupéry",
                "Fable",
                "A poetic tale about innocence, love, loss, and what truly matters.",
                "Reynal & Hitchcock",
                new DateOnly(1943, 4, 6),
                "9780156012195",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "Brave New World",
                "Aldous Huxley",
                "Dystopian",
                "A chilling vision of a society engineered for comfort and obedience.",
                "Chatto & Windus",
                new DateOnly(1932, 1, 1),
                "9780060850524",
                userId,
                adminId,
                approvedOn),

            CreateBook(
                "Crime and Punishment",
                "Fyodor Dostoevsky",
                "Psychological Fiction",
                "A tormented student confronts guilt, morality, and redemption.",
                "The Russian Messenger",
                new DateOnly(1866, 1, 1),
                "9780143058144",
                userId,
                adminId,
                approvedOn),
        };

        await data.AddRangeAsync(books, cancellationToken);
        await data.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {Count} fiction books for user {UserId} approved by admin {AdminId}.",
            books.Count,
            userId,
            adminId);
    }

    private static BookDbModel CreateBook(
        string title,
        string author,
        string genre,
        string description,
        string publisher,
        DateOnly publishedOn,
        string isbn,
        string creatorId,
        string approvedBy,
        DateTime approvedOn)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Author = author,
            NormalizedTitle = Normalize(title),
            NormalizedAuthor = Normalize(author),
            Genre = genre,
            Description = description,
            Publisher = publisher,
            PublishedOn = publishedOn,
            Isbn = isbn,
            NormalizedIsbn = Normalize(isbn),
            CreatorId = creatorId,
            IsApproved = true,
            ApprovedOn = approvedOn,
            ApprovedBy = approvedBy,
            IsDeleted = false,
        };

    private static string Normalize(string value)
        => value.Trim().ToUpperInvariant();
}