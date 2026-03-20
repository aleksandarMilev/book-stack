namespace BookStack.Tests.UserProfile.Integration;

using BookStack.Features.Identity.Data.Models;
using BookStack.Features.UserProfile.Data.Models;
using BookStack.Tests.TestInfrastructure.DataProviders;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.EntityFrameworkCore;

using static BookStack.Features.UserProfile.Shared.Constants;

public class ProfileQueryFilterIntegrationTests
{
    [Fact]
    public async Task ProfileQueryFilter_ShouldHideSoftDeletedProfiles()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var activeUser = CreateUser("active-user", "active@example.com");
        var deletedProfileUser = CreateUser("deleted-profile-user", "deleted-profile@example.com");

        data.Users.AddRange(activeUser, deletedProfileUser);
        data.Profiles.AddRange(
            new UserProfileDbModel
            {
                UserId = activeUser.Id,
                FirstName = "Active",
                LastName = "User",
                ImagePath = Paths.DefaultImagePath,
            },
            new UserProfileDbModel
            {
                UserId = deletedProfileUser.Id,
                FirstName = "Deleted",
                LastName = "Profile",
                ImagePath = Paths.DefaultImagePath,
                IsDeleted = true,
            });

        await data.SaveChangesAsync(CancellationToken.None);

        Assert.Single(await data.Profiles.ToListAsync());
        Assert.Equal(2, await data.Profiles.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task ProfileQueryFilter_ShouldHideProfilesOfSoftDeletedUsers()
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
            new UserProfileDbModel
            {
                UserId = activeUser.Id,
                FirstName = "Active",
                LastName = "User",
                ImagePath = Paths.DefaultImagePath,
            },
            new UserProfileDbModel
            {
                UserId = deletedUser.Id,
                FirstName = "Deleted",
                LastName = "User",
                ImagePath = Paths.DefaultImagePath,
            });

        await data.SaveChangesAsync(CancellationToken.None);

        Assert.Single(await data.Profiles.ToListAsync());
        Assert.Equal(2, await data.Profiles.IgnoreQueryFilters().CountAsync());
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

