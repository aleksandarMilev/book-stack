namespace BookStack.Features.Payments.Data.Configuration;

using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static Shared.Constants;

public sealed class PaymentDbModelConfiguration : IEntityTypeConfiguration<PaymentDbModel>
{
    public void Configure(EntityTypeBuilder<PaymentDbModel> builder)
    {
        builder
            .HasKey(static p => p.Id);

        builder
            .HasOne(static p => p.Order)
            .WithMany()
            .HasForeignKey(static p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(static p => p.Provider)
            .IsRequired()
            .HasMaxLength(Validation.ProviderMaxLength);

        builder
            .Property(static p => p.ProviderPaymentId)
            .IsRequired()
            .HasMaxLength(Validation.ProviderPaymentIdMaxLength);

        builder
            .Property(static p => p.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder
            .Property(static p => p.Currency)
            .IsRequired()
            .HasMaxLength(Validation.CurrencyLength)
            .IsFixedLength();

        builder
            .Property(static p => p.Status)
            .IsRequired();

        builder
            .Property(static p => p.FailureReason)
            .HasMaxLength(Validation.FailureReasonMaxLength);

        builder
            .Property(static p => p.LastProviderEventId)
            .HasMaxLength(Validation.ProviderEventIdMaxLength);

        builder
            .Property(static p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .HasIndex(static p => p.OrderId);

        builder
            .HasIndex(static p => p.Status);

        builder
            .HasIndex(static p => p.CreatedOn);

        builder
            .HasIndex(static p => new { p.Provider, p.ProviderPaymentId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
