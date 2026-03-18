namespace BookStack.Infrastructure.Outbox.Data.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

using static Common.Constants;

public sealed class OutboxMessageDbModelConfiguration
    : IEntityTypeConfiguration<OutboxMessageDbModel>
{
    public void Configure(EntityTypeBuilder<OutboxMessageDbModel> builder)
    {
        builder
            .HasKey(static m => m.Id);

        builder
            .Property(static m => m.Type)
            .IsRequired()
            .HasMaxLength(Validation.TypeMaxLength);

        builder
            .Property(static m => m.PayloadJson)
            .IsRequired();

        builder
            .Property(static m => m.OccurredOnUtc)
            .IsRequired();

        builder
            .Property(static m => m.CreatedOnUtc)
            .IsRequired();

        builder
            .Property(static m => m.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder
            .HasIndex(static m => m.ProcessedOnUtc);

        builder
            .HasIndex(static m => m.NextAttemptOnUtc);

        builder
            .HasIndex(static x => new
            {
                x.ProcessedOnUtc,
                x.NextAttemptOnUtc,
                x.OccurredOnUtc
            });

        builder
            .HasIndex(static m => m.LockedUntilUtc);

        builder
            .HasIndex(static m => new
            {
                m.ProcessedOnUtc,
                m.NextAttemptOnUtc,
                m.LockedUntilUtc,
                m.OccurredOnUtc
            });
    }
}