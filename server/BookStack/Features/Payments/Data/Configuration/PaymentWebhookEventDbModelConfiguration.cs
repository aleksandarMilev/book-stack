namespace BookStack.Features.Payments.Data.Configuration;

using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static Shared.Constants;

public sealed class PaymentWebhookEventDbModelConfiguration : IEntityTypeConfiguration<PaymentWebhookEventDbModel>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEventDbModel> builder)
    {
        builder
            .HasKey(static e => e.Id);

        builder
            .HasOne(static e => e.Payment)
            .WithMany()
            .HasForeignKey(static e => e.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(static e => e.Order)
            .WithMany()
            .HasForeignKey(static e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(static e => e.Provider)
            .IsRequired()
            .HasMaxLength(Validation.ProviderMaxLength);

        builder
            .Property(static e => e.ProviderEventId)
            .IsRequired()
            .HasMaxLength(Validation.ProviderEventIdMaxLength);

        builder
            .Property(static e => e.ProviderPaymentId)
            .HasMaxLength(Validation.ProviderPaymentIdMaxLength);

        builder
            .Property(static e => e.FailureReason)
            .HasMaxLength(Validation.FailureReasonMaxLength);

        builder
            .Property(static e => e.ProcessingResult)
            .HasMaxLength(Validation.ProcessingResultMaxLength);

        builder
            .Property(static e => e.Payload)
            .HasMaxLength(Validation.PayloadMaxLength);

        builder
            .Property(static e => e.ProcessedOnUtc)
            .IsRequired();

        builder
            .Property(static e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .HasIndex(static e => new { e.Provider, e.ProviderEventId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder
            .HasIndex(static e => e.PaymentId);

        builder
            .HasIndex(static e => e.OrderId);

        builder
            .HasIndex(static e => e.ProcessedOnUtc);
    }
}
