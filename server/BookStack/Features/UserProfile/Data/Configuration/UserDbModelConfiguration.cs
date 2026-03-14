namespace BookStack.Features.UserProfile.Data.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

using static Shared.Constants.Validation;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfileDbModel>
{
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
    }
}
