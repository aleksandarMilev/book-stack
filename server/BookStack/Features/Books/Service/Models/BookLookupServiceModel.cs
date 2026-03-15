namespace BookStack.Features.Books.Service.Models;

public class BookLookupServiceModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = default!;

    public string Author { get; init; } = default!;

    public string Genre { get; init; } = default!;

    public string? Isbn { get; init; }
}
