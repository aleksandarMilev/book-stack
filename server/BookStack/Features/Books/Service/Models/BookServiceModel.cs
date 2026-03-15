namespace BookStack.Features.Books.Service.Models;

public class BookServiceModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = default!;

    public string Author { get; init; } = default!;

    public string Genre { get; init; } = default!;

    public string? Description { get; init; }

    public string? Publisher { get; init; }

    public string? PublishedOn { get; init; }

    public string? Isbn { get; init; }

    public string CreatorId { get; init; } = default!;

    public bool IsApproved { get; init; }

    public string? RejectionReason { get; init; }

    public string CreatedOn { get; init; } = default!;

    public string? ModifiedOn { get; init; }

    public string? ApprovedOn { get; init; }

    public string? ApprovedBy { get; init; }
}
