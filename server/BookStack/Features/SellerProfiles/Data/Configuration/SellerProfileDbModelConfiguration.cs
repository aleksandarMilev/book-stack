namespace BookStack.Features.SellerProfiles.Data.Configuration;

using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static Shared.Constants.Validation;

/// <summary>
/// Configures the EF Core model for <see cref="SellerProfileDbModel"/>.
/// </summary>
/// <remarks>
/// Seller profiles are keyed by <see cref="SellerProfileDbModel.UserId"/>, include defaults for payment-support
/// and activation flags, and are hidden by a global query filter when either the profile or linked user is soft-deleted.
/// </remarks>
public sealed class SellerProfileDbModelConfiguration : IEntityTypeConfiguration<SellerProfileDbModel>
{
    /// <summary>
    /// Configures schema constraints, defaults, indexes, and query-filter behavior for seller profiles.
    /// </summary>
    /// <param name="builder">Entity type builder for <see cref="SellerProfileDbModel"/>.</param>
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

        builder
            .HasQueryFilter(static p => !p.IsDeleted && !p.User.IsDeleted);
    }
}
