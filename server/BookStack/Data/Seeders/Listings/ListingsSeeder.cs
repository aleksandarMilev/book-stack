namespace BookStack.Data.Seeders.Listings;

using Areas.Admin.Service;
using BookStack.Features.BookListings.Data.Models;
using BookStack.Features.BookListings.Shared;
using BookStack.Features.Books.Data.Models;
using Infrastructure.Services.DateTimeProvider;
using Microsoft.EntityFrameworkCore;

public sealed class ListingsSeeder(
    BookStackDbContext data,
    IAdminService adminService,
    IDateTimeProvider dateTimeProvider,
    ILogger<ListingsSeeder> logger) : IListingsSeeder
{
    private const string ZeroOrMoreThanOneSellersErrorMessage =
        "There should be exactly one seller user in dev environment so book listings seeding works correctly!";

    public async Task Seed(CancellationToken cancellationToken)
    {
        string? sellerId;

        try
        {
            sellerId = await data
                .SellerProfiles
                .AsNoTracking()
                .Select(static s => s.UserId)
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning(ZeroOrMoreThanOneSellersErrorMessage);
            throw new InvalidOperationException(ZeroOrMoreThanOneSellersErrorMessage);
        }

        if (sellerId is null)
        {
            logger.LogWarning(ZeroOrMoreThanOneSellersErrorMessage);
            throw new InvalidOperationException(ZeroOrMoreThanOneSellersErrorMessage);
        }

        var adminId = await adminService.GetId();

        var hasListings = await data
            .BookListings
            .AsNoTracking()
            .AnyAsync(cancellationToken);

        if (hasListings)
        {
            logger.LogInformation("Listings already exist. Skipping listings seed.");
            return;
        }

        var books = await data
            .Books
            .AsNoTracking()
            .OrderBy(static b => b.Title)
            .ToListAsync(cancellationToken);

        if (books.Count == 0)
        {
            throw new InvalidOperationException(
                "Books must be seeded before listings are seeded.");
        }

        var approvedOn = dateTimeProvider.UtcNow;

        var listings = books
            .Select(book => CreateListing(
                book,
                sellerId,
                adminId,
                approvedOn))
            .ToList();

        await data.AddRangeAsync(listings, cancellationToken);
        await data.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {Count} listings for seller {SellerId} approved by admin {AdminId}.",
            listings.Count,
            sellerId,
            adminId);
    }

    private static BookListingDbModel CreateListing(
        BookDbModel book,
        string sellerId,
        string adminId,
        DateTime approvedOn)
        => new()
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            CreatorId = sellerId,
            Price = GetPrice(book.Title),
            Currency = "EU",
            Condition = GetCondition(book.Title),
            Quantity = GetQuantity(book.Title),
            Description = GetDescription(book),
            ImagePath = "/images/listings/default.jpg",
            IsApproved = true,
            ApprovedOn = approvedOn,
            ApprovedBy = adminId,
            IsDeleted = false,
        };

    private static decimal GetPrice(string title)
        => title switch
        {
            "1984" => 10.90m,
            "Brave New World" => 11.40m,
            "Crime and Punishment" => 12.80m,
            "Harry Potter and the Philosopher's Stone" => 16.50m,
            "Pride and Prejudice" => 9.20m,
            "The Alchemist" => 10.30m,
            "The Catcher in the Rye" => 10.70m,
            "The Great Gatsby" => 9.80m,
            "The Hobbit" => 14.20m,
            "The Little Prince" => 8.60m,
            "The Lord of the Rings" => 22.90m,
            "To Kill a Mockingbird" => 11.90m,
            _ => 9.99m
        };

    private static ListingCondition GetCondition(string title)
        => title switch
        {
            "The Lord of the Rings" => ListingCondition.LikeNew,
            "The Little Prince" => ListingCondition.LikeNew,
            "Harry Potter and the Philosopher's Stone" => ListingCondition.VeryGood,
            "The Hobbit" => ListingCondition.VeryGood,
            "Crime and Punishment" => ListingCondition.VeryGood,
            "To Kill a Mockingbird" => ListingCondition.VeryGood,
            "Pride and Prejudice" => ListingCondition.Acceptable,
            _ => ListingCondition.Good
        };

    private static int GetQuantity(string title)
        => title switch
        {
            "Harry Potter and the Philosopher's Stone" => 3,
            "The Hobbit" => 2,
            "The Lord of the Rings" => 2,
            _ => 1
        };

    private static string GetDescription(BookDbModel book)
        => book.Title switch
        {
            "The Great Gatsby" =>
                "Good used copy with light shelf wear. Clean pages and solid binding.",

            "To Kill a Mockingbird" =>
                "Very good condition. Clean interior, minimal wear, and no missing pages.",

            "1984" =>
                "Used copy in good condition. Some edge wear but fully intact and readable.",

            "Pride and Prejudice" =>
                "Readable older copy with visible wear. A solid budget edition of a classic.",

            "The Catcher in the Rye" =>
                "Good condition with moderate signs of use. Binding remains strong.",

            "The Hobbit" =>
                "Very good copy with clean pages and minor cosmetic wear only.",

            "The Lord of the Rings" =>
                "Like new condition. Clean, sturdy, and excellent for gifting or collecting.",

            "Harry Potter and the Philosopher's Stone" =>
                "Very good used condition with clean pages and light shelf wear.",

            "The Alchemist" =>
                "Good paperback copy with tight binding and clean pages.",

            "The Little Prince" =>
                "Like new condition. Crisp pages and beautifully preserved.",

            "Brave New World" =>
                "Good used condition with light reading wear and no writing inside.",

            "Crime and Punishment" =>
                "Very good condition. Clean pages, strong binding, and minor storage wear.",

            _ =>
                $"Good used copy of {book.Title} by {book.Author}. Clean pages and intact binding."
        };

    // Will be used once the actual image are added in the wwwroot folder
    private static string GetImagePath(string title)
        => title switch
        {
            "The Great Gatsby" => "/images/books/the-great-gatsby.jpg",
            "To Kill a Mockingbird" => "/images/books/to-kill-a-mockingbird.jpg",
            "1984" => "/images/books/1984.jpg",
            "Pride and Prejudice" => "/images/books/pride-and-prejudice.jpg",
            "The Catcher in the Rye" => "/images/books/the-catcher-in-the-rye.jpg",
            "The Hobbit" => "/images/books/the-hobbit.jpg",
            "The Lord of the Rings" => "/images/books/the-lord-of-the-rings.jpg",
            "Harry Potter and the Philosopher's Stone" => "/images/books/harry-potter-and-the-philosophers-stone.jpg",
            "The Alchemist" => "/images/books/the-alchemist.jpg",
            "The Little Prince" => "/images/books/the-little-prince.jpg",
            "Brave New World" => "/images/books/brave-new-world.jpg",
            "Crime and Punishment" => "/images/books/crime-and-punishment.jpg",
            _ => "/images/books/default.jpg"
        };
}