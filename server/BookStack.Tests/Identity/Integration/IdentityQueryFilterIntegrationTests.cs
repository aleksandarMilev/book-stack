namespace BookStack.Tests.Identity.Integration;

using BookStack.Features.Identity.Data.Models;
using BookStack.Features.SellerProfiles.Data.Models;
using BookStack.Features.UserProfile.Data.Models;
using BookStack.Tests.TestInfrastructure.DataProviders;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.EntityFrameworkCore;

public class IdentityQueryFilterIntegrationTests
{
    [Fact]
    public async Task UserQueryFilter_ShouldHideSoftDeletedUsers()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        data.Users.AddRange(
            new()
            {
                UserName = "active-user",
                NormalizedUserName = "ACTIVE-USER",
                Email = "active@example.com",
                NormalizedEmail = "ACTIVE@EXAMPLE.COM",
                SecurityStamp = Guid.NewGuid().ToString(),
            },
            new()
            {
                UserName = "deleted-user",
                NormalizedUserName = "DELETED-USER",
                Email = "deleted@example.com",
                NormalizedEmail = "DELETED@EXAMPLE.COM",
                SecurityStamp = Guid.NewGuid().ToString(),
                IsDeleted = true,
            });

        await data.SaveChangesAsync(CancellationToken.None);

        Assert.Single(await data.Users.ToListAsync());
        Assert.Equal(2, await data.Users.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task ProfileQueryFilter_ShouldHideProfilesOfDeletedUsers()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var activeUser = CreateUser("active-user", "active@example.com");

        var deletedUser = CreateUser(
            "deleted-user",
            "deleted@example.com",
            isDeleted: true);

        data.Users.AddRange(activeUser, deletedUser);
        data.Profiles.AddRange(
            new()
            {
                UserId = activeUser.Id,
                FirstName = "Active",
                LastName = "User",
                ImagePath = "/images/profiles/default.webp"
            },
            new()
            {
                UserId = deletedUser.Id,
                FirstName = "Deleted",
                LastName = "User",
                ImagePath = "/images/profiles/default.webp"
            });

        await data.SaveChangesAsync(CancellationToken.None);

        Assert.Single(await data.Profiles.ToListAsync());
        Assert.Equal(2, await data.Profiles.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task SellerProfileQueryFilter_ShouldHideSellerProfilesOfDeletedUsers()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var activeUser = CreateUser(
            "seller-active",
            "seller-active@example.com");

        var deletedUser = CreateUser(
            "seller-deleted",
            "seller-deleted@example.com",
            isDeleted: true);

        data.Users.AddRange(activeUser, deletedUser);
        data.SellerProfiles.AddRange(
            new()
            {
                UserId = activeUser.Id,
                DisplayName = "Active Seller",
            },
            new()
            {
                UserId = deletedUser.Id,
                DisplayName = "Deleted Seller",
            });

        await data.SaveChangesAsync(CancellationToken.None);

        Assert.Single(await data.SellerProfiles.ToListAsync());
        Assert.Equal(2, await data.SellerProfiles.IgnoreQueryFilters().CountAsync());
    }

    private static UserDbModel CreateUser(
        string username,
        string email,
        bool isDeleted = false)
        => new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            IsDeleted = isDeleted,
        };
}
