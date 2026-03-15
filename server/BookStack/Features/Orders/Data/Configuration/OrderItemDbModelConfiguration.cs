namespace BookStack.Features.Orders.Data.Configuration;

using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static Shared.Constants;

public sealed class OrderItemDbModelConfiguration : IEntityTypeConfiguration<OrderItemDbModel>
{
    public void Configure(EntityTypeBuilder<OrderItemDbModel> builder)
    {
        builder
            .HasKey(static i => i.Id);

        builder
            .HasOne(static i => i.Listing)
            .WithMany()
            .HasForeignKey(static i => i.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(static i => i.Book)
            .WithMany()
            .HasForeignKey(static i => i.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(static i => i.SellerId)
            .IsRequired();

        builder
            .Property(static i => i.BookTitle)
            .IsRequired()
            .HasMaxLength(Validation.BookTitleMaxLength);

        builder
            .Property(static i => i.BookAuthor)
            .IsRequired()
            .HasMaxLength(Validation.BookAuthorMaxLength);

        builder
            .Property(static i => i.BookGenre)
            .IsRequired()
            .HasMaxLength(Validation.BookGenreMaxLength);

        builder
            .Property(static i => i.BookPublisher)
            .HasMaxLength(Validation.BookPublisherMaxLength);

        builder
            .Property(static i => i.BookPublishedOn)
            .HasMaxLength(Validation.DateStringMaxLength);

        builder
            .Property(static i => i.BookIsbn)
            .HasMaxLength(Validation.BookIsbnMaxLength);

        builder
            .Property(static i => i.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder
            .Property(static i => i.TotalPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder
            .Property(static i => i.Quantity)
            .IsRequired();

        builder
            .Property(static i => i.Currency)
            .IsRequired()
            .HasMaxLength(Validation.CurrencyMaxLength)
            .IsFixedLength();

        builder
            .Property(static i => i.Condition)
            .IsRequired();

        builder
            .Property(static i => i.ListingDescription)
            .IsRequired()
            .HasMaxLength(Validation.ListingDescriptionMaxLength);

        builder
            .Property(static i => i.ListingImagePath)
            .IsRequired()
            .HasMaxLength(Common.Constants.Validation.ImagePathMaxLength);

        builder
            .Property(static i => i.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .HasIndex(static i => i.OrderId);

        builder
            .HasIndex(static i => i.ListingId);

        builder
            .HasIndex(static i => i.BookId);

        builder
            .HasIndex(static i => i.SellerId);
    }
}
