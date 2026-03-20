namespace BookStack.Features.UserProfile.Data.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

using static Shared.Constants.Validation;

/// <summary>
/// Configures the EF Core model for <see cref="UserProfileDbModel"/>.
/// </summary>
/// <remarks>
/// The profile is keyed by <see cref="UserProfileDbModel.UserId"/> and is hidden by a global query filter when
/// either the profile itself or its linked user is soft-deleted.
/// </remarks>
public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfileDbModel>
{
    /// <summary>
    /// Configures schema constraints and query-filter behavior for user profiles.
    /// </summary>
    /// <param name="builder">Entity type builder for <see cref="UserProfileDbModel"/>.</param>
    public void Configure(EntityTypeBuilder<UserProfileDbModel> builder)
    {
        builder
            .HasKey(static p => p.UserId);

        builder
            .Property(static p => p.UserId)
            .IsRequired();

        builder
            .Property(static p => p.FirstName)
            .IsRequired()
            .HasMaxLength(NameMaxLength);

        builder
            .Property(static p => p.LastName)
            .IsRequired()
            .HasMaxLength(NameMaxLength);

        builder
            .Property(static p => p.ImagePath)
            .IsRequired();

        builder
            .Property(static p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder
            .HasQueryFilter(static p => !p.IsDeleted && !p.User.IsDeleted);
    }
}
