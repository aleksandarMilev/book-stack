namespace BookStack.Features.Books.Web.Models;

using System.ComponentModel.DataAnnotations;

using static Shared.Constants.Validation;

public class CreateBookWebModel
{
    [Required]
    [StringLength(
        TitleMaxLength,
        MinimumLength = TitleMinLength)]
    public string Title { get; init; } = default!;

    [Required]
    [StringLength(
        AuthorMaxLength,
        MinimumLength = AuthorMinLength)]
    public string Author { get; init; } = default!;

    [Required]
    [StringLength(
        GenreMaxLength,
        MinimumLength = GenreMinLength)]
    public string Genre { get; init; } = default!;

    [StringLength(
        DescriptionMaxLength,
        MinimumLength = DescriptionMinLength)]
    public string? Description { get; init; }

    [StringLength(
        PublisherMaxLength,
        MinimumLength = PublisherMinLength)]
    public string? Publisher { get; init; }

    public DateOnly? PublishedOn { get; init; }

    [StringLength(
        IsbnMaxLength,
        MinimumLength = IsbnMinLength)]
    public string? Isbn { get; init; }
}