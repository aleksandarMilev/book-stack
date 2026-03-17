namespace BookStack.Features.Identity.Data.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;
using SellerProfiles.Data.Models;
using UserProfile.Data.Models;

public sealed class UserDbModelConfiguration : IEntityTypeConfiguration<UserDbModel>
{
    public void Configure(EntityTypeBuilder<UserDbModel> builder)
    {
        builder
            .HasOne(static u => u.Profile)
            .WithOne(static p => p.User)
            .HasForeignKey<UserProfileDbModel>(static p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(static u => u.SellerProfile)
            .WithOne(static p => p.User)
            .HasForeignKey<SellerProfileDbModel>(static p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(static u => u.IsDeleted)
            .HasDefaultValue(false);

        builder
            .HasQueryFilter(static u => !u.IsDeleted);

        builder
            .HasIndex(static u => u.IsDeleted);
    }
}
