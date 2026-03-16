namespace BookStack.Features.Books.Data.Models;

using BookStack.Data.Models.Base;

public class BookDbModel:
    DeletableEntity<Guid>,
    IApprovableEntity
{
    public string Title { get; set; } = default!;

    public string Author { get; set; } = default!;

    public string NormalizedTitle { get; set; } = default!;

    public string NormalizedAuthor { get; set; } = default!;

    public string Genre { get; set; } = default!;

    public string? Description { get; set; }

    public string? Publisher { get; set; }

    public DateOnly? PublishedOn { get; set; }

    public string? Isbn { get; set; }

    public string? NormalizedIsbn { get; set; }

    public string CreatorId { get; set; } = default!;

    public bool IsApproved { get; set; }

    public DateTime? ApprovedOn { get; set; }

    public string? ApprovedBy { get; set; }

    public string? RejectionReason { get; set; }
}
