namespace BookStack.Features.Books.Service.Models;

public class CreateBookServiceModel
{
    public string Title { get; init; } = default!;

    public string Author { get; init; } = default!;

    public string Genre { get; init; } = default!;

    public string? Description { get; init; }

    public string? Publisher { get; init; }

    public DateOnly? PublishedOn { get; init; }

    public string? Isbn { get; init; }
}
