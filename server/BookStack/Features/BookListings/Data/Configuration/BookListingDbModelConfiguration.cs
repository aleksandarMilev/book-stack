namespace BookStack.Features.BookListings.Data.Configuration;

using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static Shared.Constants;

public sealed class BookListingDbModelConfiguration : IEntityTypeConfiguration<BookListingDbModel>
{
    public void Configure(EntityTypeBuilder<BookListingDbModel> builder)
    {
        builder
            .HasKey(static l => l.Id);

        builder
            .HasOne(static l => l.Book)
            .WithMany()
            .HasForeignKey(static l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(static l => l.CreatorId)
            .IsRequired();

        builder
            .Property(static l => l.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder
            .Property(static l => l.Currency)
            .IsRequired()
            .HasMaxLength(Validation.CurrencyMaxLength)
            .IsFixedLength();

        builder
            .Property(static l => l.Condition)
            .IsRequired();

        builder
            .Property(static l => l.Quantity)
            .IsRequired();

        builder
            .Property(static l => l.Description)
            .IsRequired()
            .HasMaxLength(Validation.DescriptionMaxLength);

        builder
            .Property(static l => l.ImagePath)
            .IsRequired()
            .HasMaxLength(Common.Constants.Validation.ImagePathMaxLength);

        builder
            .Property(static l => l.IsApproved)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .Property(static l => l.RejectionReason)
            .HasMaxLength(Validation.RejectionReasonMaxLength);

        builder
            .Property(static l => l.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .HasIndex(static l => l.BookId);

        builder
            .HasIndex(static l => l.CreatorId);

        builder
            .HasIndex(static l => l.IsApproved);

        builder
            .HasIndex(static l => l.Price);

        builder
            .HasIndex(static l => l.Condition);

        builder
            .HasIndex(static l => l.CreatedOn);
    }
}