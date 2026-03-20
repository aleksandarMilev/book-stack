namespace BookStack.Tests.UserProfile.Integration;

using BookStack.Features.Identity.Data.Models;
using BookStack.Features.UserProfile.Data.Models;
using BookStack.Tests.TestInfrastructure.DataProviders;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.EntityFrameworkCore;

using static BookStack.Common.Constants;
using static BookStack.Features.UserProfile.Shared.Constants;

public class ProfileServiceIntegrationTests
{
    [Fact]
    public async Task Mine_ShouldReturnCurrentUserProfile()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com",
            firstName: "Alice",
            lastName: "Tester");

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .ProfileService
            .Mine(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
        Assert.Equal("Alice", result.FirstName);
        Assert.Equal("Tester", result.LastName);
        Assert.Equal(Paths.DefaultImagePath, result.ImagePath);
    }

    [Fact]
    public async Task Mine_ShouldReturnNull_WhenCurrentUserIsSoftDeleted()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        SetCurrentUser(
            currentUserService,
            user);

        user.IsDeleted = true;
        user.DeletedOn = dateTimeProvider.UtcNow;

        var updateResult = await testFactory
            .UserManager
            .UpdateAsync(user);

        Assert.True(
            updateResult.Succeeded,
            string.Join("; ", updateResult.Errors.Select(static e => e.Description)));

        var result = await testFactory
            .ProfileService
            .Mine(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Edit_ShouldReturnNotFound_WhenCurrentUserProfileDoesNotExist()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var user = await testFactory.CreateUser(
            username: "alice",
            email: "alice@example.com");

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .ProfileService
            .Edit(
                UserProfileTestData.CreateServiceModel(),
                CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            string.Format(
                ErrorMessages.DbEntityNotFound,
                nameof(UserProfileDbModel),
                user.Id),
            result.ErrorMessage);
    }

    [Fact]
    public async Task Edit_ShouldUpdateFirstNameAndLastName()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com",
            firstName: "Alice",
            lastName: "Tester");

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .ProfileService
            .Edit(
                UserProfileTestData.CreateServiceModel(
                    firstName: "Alicia",
                    lastName: "Updated"),
                CancellationToken.None);

        Assert.True(result.Succeeded);

        var profile = await data
            .Profiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        Assert.Equal("Alicia", profile.FirstName);
        Assert.Equal("Updated", profile.LastName);
    }

    [Fact]
    public async Task Edit_ShouldSetDefaultImagePath_WhenRemoveImageIsTrue()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com",
            imagePath: "/images/profiles/custom-old.jpg");

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .ProfileService
            .Edit(
                UserProfileTestData.CreateServiceModel(
                    removeImage: true),
                CancellationToken.None);

        Assert.True(result.Succeeded);

        var profile = await data
            .Profiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        Assert.Equal(Paths.DefaultImagePath, profile.ImagePath);

        var deleteCall = Assert.Single(testFactory.ImageWriter.DeleteCalls);
        Assert.Equal(Paths.ProfilesImagePathPrefix, deleteCall.ResourceName);
        Assert.Equal("/images/profiles/custom-old.jpg", deleteCall.ImagePath);
        Assert.Equal(Paths.DefaultImagePath, deleteCall.DefaultImagePath);
    }

    [Fact]
    public async Task Edit_ShouldReplaceImagePath_AndDeleteOldImage_WhenNewImageIsUploaded()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com",
            imagePath: "/images/profiles/old-upload.jpg");

        SetCurrentUser(
            currentUserService,
            user);

        const string expectedNewImagePath = "/images/profiles/new-upload.jpg";
        testFactory.ImageWriter.NextUploadedImagePaths.Enqueue(expectedNewImagePath);

        var result = await testFactory
            .ProfileService
            .Edit(
                UserProfileTestData.CreateServiceModel(
                    image: UserProfileTestData.CreateImage()),
                CancellationToken.None);

        Assert.True(result.Succeeded);

        var profile = await data
            .Profiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        Assert.Equal(expectedNewImagePath, profile.ImagePath);

        var writeCall = Assert.Single(testFactory.ImageWriter.WriteCalls);
        Assert.Equal(Paths.ProfilesImagePathPrefix, writeCall.ResourceName);
        Assert.True(writeCall.HasImage);
        Assert.Null(writeCall.DefaultImagePath);

        var deleteCall = Assert.Single(testFactory.ImageWriter.DeleteCalls);
        Assert.Equal(Paths.ProfilesImagePathPrefix, deleteCall.ResourceName);
        Assert.Equal("/images/profiles/old-upload.jpg", deleteCall.ImagePath);
        Assert.Equal(Paths.DefaultImagePath, deleteCall.DefaultImagePath);
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteProfile()
    {
        await using var database = new SqliteTestDatabase();

        var utc = new DateTime(2026, 03, 19, 10, 00, 00, DateTimeKind.Utc);
        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(utc);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .ProfileService
            .Delete(cancellationToken: CancellationToken.None);

        Assert.True(result.Succeeded);

        var profile = await data
            .Profiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        Assert.True(profile.IsDeleted);
        Assert.Equal(dateTimeProvider.UtcNow, profile.DeletedOn);
        Assert.Equal(user.UserName, profile.DeletedBy);
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteUnderlyingUser()
    {
        await using var database = new SqliteTestDatabase();

        var utc = new DateTime(2026, 03, 19, 10, 00, 00, DateTimeKind.Utc);
        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(utc);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .ProfileService
            .Delete(cancellationToken: CancellationToken.None);

        Assert.True(result.Succeeded);

        var deletedUser = await data
            .Users
            .IgnoreQueryFilters()
            .SingleAsync(
                u => u.Id == user.Id,
                CancellationToken.None);

        Assert.True(deletedUser.IsDeleted);
        Assert.Equal(dateTimeProvider.UtcNow, deletedUser.DeletedOn);
        Assert.Equal(user.UserName, deletedUser.DeletedBy);
        Assert.Equal(DateTimeOffset.MaxValue, deletedUser.LockoutEnd);
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenDeletingAnotherUserAndCurrentUserIsNotAdmin()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (currentUser, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        var (targetUser, _) = await testFactory.CreateUserWithProfile(
            username: "bob",
            email: "bob@example.com");

        SetCurrentUser(
            currentUserService,
            currentUser,
            isAdmin: false);

        var result = await testFactory
            .ProfileService
            .Delete(
                userId: targetUser.Id,
                cancellationToken: CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            string.Format(
                ErrorMessages.UnauthorizedMessage,
                currentUser.Id,
                nameof(UserProfileDbModel),
                targetUser.Id),
            result.ErrorMessage);

        var persistedTargetProfile = await data
            .Profiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == targetUser.Id,
                CancellationToken.None);

        var persistedTargetUser = await data
            .Users
            .IgnoreQueryFilters()
            .SingleAsync(
                u => u.Id == targetUser.Id,
                CancellationToken.None);

        Assert.False(persistedTargetProfile.IsDeleted);
        Assert.False(persistedTargetUser.IsDeleted);
    }

    [Fact]
    public async Task Delete_ShouldAllowAdminToDeleteAnotherUser()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new UserProfileTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (adminUser, _) = await testFactory.CreateUserWithProfile(
            username: "admin",
            email: "admin@example.com");

        var (targetUser, _) = await testFactory.CreateUserWithProfile(
            username: "bob",
            email: "bob@example.com");

        SetCurrentUser(
            currentUserService,
            adminUser,
            isAdmin: true);

        var result = await testFactory
            .ProfileService
            .Delete(
                userId: targetUser.Id,
                cancellationToken: CancellationToken.None);

        Assert.True(result.Succeeded);

        var persistedTargetProfile = await data
            .Profiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == targetUser.Id,
                CancellationToken.None);

        var persistedTargetUser = await data
            .Users
            .IgnoreQueryFilters()
            .SingleAsync(
                u => u.Id == targetUser.Id,
                CancellationToken.None);

        Assert.True(persistedTargetProfile.IsDeleted);
        Assert.True(persistedTargetUser.IsDeleted);
    }

    private static void SetCurrentUser(
        FakeCurrentUserService currentUserService,
        UserDbModel user,
        bool isAdmin = false)
    {
        currentUserService.UserId = user.Id;
        currentUserService.Username = user.UserName;
        currentUserService.Admin = isAdmin;
    }
}
