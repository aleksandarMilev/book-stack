namespace BookStack.Features.Books.Data.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

using static Shared.Constants;

public sealed class BookDbModelConfiguration : IEntityTypeConfiguration<BookDbModel>
{
    public void Configure(EntityTypeBuilder<BookDbModel> builder)
    {
        builder
            .HasKey(static b => b.Id);

        builder
            .Property(static b => b.Title)
            .IsRequired()
            .HasMaxLength(Validation.TitleMaxLength);

        builder
            .Property(static b => b.Author)
            .IsRequired()
            .HasMaxLength(Validation.AuthorMaxLength);

        builder
            .Property(static b => b.NormalizedTitle)
            .IsRequired()
            .HasMaxLength(Validation.TitleMaxLength);

        builder
            .Property(static b => b.NormalizedAuthor)
            .IsRequired()
            .HasMaxLength(Validation.AuthorMaxLength);

        builder
            .Property(static b => b.Genre)
            .IsRequired()
            .HasMaxLength(Validation.GenreMaxLength);

        builder
            .Property(static b => b.Description)
            .HasMaxLength(Validation.DescriptionMaxLength);

        builder
            .Property(static b => b.Publisher)
            .HasMaxLength(Validation.PublisherMaxLength);

        builder
            .Property(static b => b.Isbn)
            .HasMaxLength(Validation.IsbnMaxLength);

        builder
            .Property(static b => b.NormalizedIsbn)
            .HasMaxLength(Validation.IsbnMaxLength);

        builder
            .Property(static b => b.CreatorId)
            .IsRequired();

        builder
            .Property(static b => b.IsApproved)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .Property(static b => b.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .HasIndex(static b => b.Title);

        builder
            .HasIndex(static b => b.Author);

        builder
            .HasIndex(static b => b.Genre);

        builder
            .HasIndex(static b => b.Publisher);

        builder
            .HasIndex(static b => b.Isbn);

        builder
            .HasIndex(static b => b.NormalizedIsbn)
            .IsUnique()
            .HasFilter("[NormalizedIsbn] IS NOT NULL AND [IsDeleted] = 0");

        builder
            .HasIndex(static b => new { b.NormalizedTitle, b.NormalizedAuthor })
            .IsUnique()
            .HasFilter("[NormalizedIsbn] IS NULL AND [IsDeleted] = 0");

        builder
            .HasIndex(static b => b.CreatorId);

        builder
            .HasIndex(static b => b.IsApproved);

        builder
            .HasIndex(static b => b.PublishedOn);
    }
}
