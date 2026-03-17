namespace BookStack.Features.SellerProfiles.Data.Configuration;

using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static Shared.Constants.Validation;

public sealed class SellerProfileDbModelConfiguration : IEntityTypeConfiguration<SellerProfileDbModel>
{
    public void Configure(EntityTypeBuilder<SellerProfileDbModel> builder)
    {
        builder
            .HasKey(static p => p.UserId);

        builder
            .Property(static p => p.UserId)
            .IsRequired();

        builder
            .Property(static p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(DisplayNameMaxLength);

        builder
            .Property(static p => p.PhoneNumber)
            .HasMaxLength(PhoneMaxLength);

        builder
            .Property(static p => p.SupportsOnlinePayment)
            .IsRequired()
            .HasDefaultValue(true);

        builder
            .Property(static p => p.SupportsCashOnDelivery)
            .IsRequired()
            .HasDefaultValue(true);

        builder
            .Property(static p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder
            .Property(static p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .HasIndex(static p => p.IsActive);

        builder
            .HasIndex(static p => p.CreatedOn);
    }
}
