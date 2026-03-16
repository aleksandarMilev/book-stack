namespace BookStack.Features.Orders.Data.Configuration;

using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static Shared.Constants;

public sealed class OrderDbModelConfiguration : IEntityTypeConfiguration<OrderDbModel>
{
    public void Configure(EntityTypeBuilder<OrderDbModel> builder)
    {
        builder
            .HasKey(static o => o.Id);

        builder
            .Property(static o => o.CustomerFirstName)
            .IsRequired()
            .HasMaxLength(Validation.NameMaxLength);

        builder
            .Property(static o => o.CustomerLastName)
            .IsRequired()
            .HasMaxLength(Validation.NameMaxLength);

        builder
            .Property(static o => o.Email)
            .IsRequired()
            .HasMaxLength(Validation.EmailMaxLength);

        builder
            .Property(static o => o.PhoneNumber)
            .HasMaxLength(Validation.PhoneMaxLength);

        builder
            .Property(static o => o.Country)
            .IsRequired()
            .HasMaxLength(Validation.CountryMaxLength);

        builder
            .Property(static o => o.City)
            .IsRequired()
            .HasMaxLength(Validation.CityMaxLength);

        builder
            .Property(static o => o.AddressLine)
            .IsRequired()
            .HasMaxLength(Validation.AddressMaxLength);

        builder
            .Property(static o => o.PostalCode)
            .HasMaxLength(Validation.PostalCodeMaxLength);

        builder
            .Property(static o => o.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder
            .Property(static o => o.Currency)
            .IsRequired()
            .HasMaxLength(Validation.CurrencyMaxLength)
            .IsFixedLength();

        builder
            .Property(static o => o.Status)
            .IsRequired();

        builder
            .Property(static o => o.PaymentStatus)
            .IsRequired();

        builder
            .Property(static o => o.GuestPaymentTokenHash)
            .HasMaxLength(Validation.GuestPaymentTokenHashLength);

        builder
            .Property(static o => o.ReservationExpiresOnUtc)
            .IsRequired();

        builder
            .Property(static o => o.ReservationReleasedOnUtc);

        builder
            .Property(static o => o.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .HasMany(static o => o.Items)
            .WithOne(static i => i.Order)
            .HasForeignKey(static i => i.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(static o => o.BuyerId);

        builder
            .HasIndex(static o => o.Email);

        builder
            .HasIndex(static o => o.Status);

        builder
            .HasIndex(static o => o.PaymentStatus);

        builder
            .HasIndex(static o => o.ReservationExpiresOnUtc);

        builder
            .HasIndex(static o => o.CreatedOn);
    }
}
