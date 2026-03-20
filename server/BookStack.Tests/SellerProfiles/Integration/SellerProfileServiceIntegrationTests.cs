namespace BookStack.Tests.SellerProfiles.Integration;

using BookStack.Features.Identity.Data.Models;
using BookStack.Features.SellerProfiles.Data.Models;
using BookStack.Tests.SellerProfiles;
using BookStack.Tests.TestInfrastructure.DataProviders;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.EntityFrameworkCore;

using static BookStack.Common.Constants;

public class SellerProfileServiceIntegrationTests
{
    [Fact]
    public async Task Mine_ShouldReturnCurrentUserSellerProfile()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        await testFactory.CreateSellerProfile(
            user,
            displayName: "Alice Seller",
            supportsOnlinePayment: true,
            supportsCashOnDelivery: false);

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .SellerProfileService
            .Mine(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.UserId);
        Assert.Equal("Alice Seller", result.DisplayName);
        Assert.True(result.SupportsOnlinePayment);
        Assert.False(result.SupportsCashOnDelivery);
    }

    [Fact]
    public async Task Mine_ShouldReturnNull_WhenCurrentUserHasNoSellerProfile()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
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
            .SellerProfileService
            .Mine(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Mine_ShouldReturnNull_WhenSellerProfileIsSoftDeleted()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        await testFactory.CreateSellerProfile(
            user,
            isDeleted: true);

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .SellerProfileService
            .Mine(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertMine_ShouldReturnFailure_WhenCurrentUserIsNotAuthenticated()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var result = await testFactory
            .SellerProfileService
            .UpsertMine(
                SellerProfilesTestData.CreateServiceModel(),
                CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            ErrorMessages.CurrentUserNotAuthenticated,
            result.ErrorMessage);
    }

    [Fact]
    public async Task UpsertMine_ShouldCreateProfile_WhenUserHasUserProfileAndNoSellerProfile()
    {
        await using var database = new SqliteTestDatabase();

        var utc = new DateTime(2026, 03, 19, 10, 00, 00, DateTimeKind.Utc);
        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(utc);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        SetCurrentUser(
            currentUserService,
            user);

        var model = SellerProfilesTestData.CreateServiceModel(
            displayName: "Alice Seller",
            phoneNumber: "+359888123456",
            supportsOnlinePayment: true,
            supportsCashOnDelivery: true);

        var result = await testFactory
            .SellerProfileService
            .UpsertMine(
                model,
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data!.UserId);
        Assert.True(result.Data.IsActive);

        var dbModel = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        Assert.Equal("Alice Seller", dbModel.DisplayName);
        Assert.Equal("+359888123456", dbModel.PhoneNumber);
        Assert.True(dbModel.SupportsOnlinePayment);
        Assert.True(dbModel.SupportsCashOnDelivery);
        Assert.True(dbModel.IsActive);
        Assert.Equal(dateTimeProvider.UtcNow, dbModel.CreatedOn);
    }

    [Fact]
    public async Task UpsertMine_ShouldUpdateExistingProfileFields()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        await testFactory.CreateSellerProfile(
            user,
            displayName: "Old Name",
            phoneNumber: "+359111111111",
            supportsOnlinePayment: true,
            supportsCashOnDelivery: false,
            isActive: true);

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .SellerProfileService
            .UpsertMine(
                SellerProfilesTestData.CreateServiceModel(
                    displayName: "Updated Seller",
                    phoneNumber: "+359999999999",
                    supportsOnlinePayment: false,
                    supportsCashOnDelivery: true),
                CancellationToken.None);

        Assert.True(result.Succeeded);

        var dbModel = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        Assert.Equal("Updated Seller", dbModel.DisplayName);
        Assert.Equal("+359999999999", dbModel.PhoneNumber);
        Assert.False(dbModel.SupportsOnlinePayment);
        Assert.True(dbModel.SupportsCashOnDelivery);
        Assert.True(dbModel.IsActive);
        Assert.NotNull(dbModel.ModifiedOn);
    }

    [Fact]
    public async Task UpsertMine_ShouldTrimDisplayNameAndPhoneNumber()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
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
            .SellerProfileService
            .UpsertMine(
                SellerProfilesTestData.CreateServiceModel(
                    displayName: "  Alice Seller  ",
                    phoneNumber: "  +359888123456  "),
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal("Alice Seller", result.Data!.DisplayName);
        Assert.Equal("+359888123456", result.Data.PhoneNumber);
    }

    [Fact]
    public async Task UpsertMine_ShouldAllowNullPhoneNumber()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
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
            .SellerProfileService
            .UpsertMine(
                SellerProfilesTestData.CreateServiceModel(
                    phoneNumber: null),
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Null(result.Data!.PhoneNumber);
    }

    [Fact]
    public async Task UpsertMine_ShouldNormalizeBlankPhoneNumberToNull()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
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
            .SellerProfileService
            .UpsertMine(
                SellerProfilesTestData.CreateServiceModel(
                    phoneNumber: "   "),
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Null(result.Data!.PhoneNumber);
    }

    [Fact]
    public async Task UpsertMine_ShouldReject_WhenNoPaymentMethodsAreSupported()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
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
            .SellerProfileService
            .UpsertMine(
                SellerProfilesTestData.CreateServiceModel(
                    supportsOnlinePayment: false,
                    supportsCashOnDelivery: false),
                CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Seller profile must support at least one payment method.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task UpsertMine_ShouldReject_WhenCurrentUserDoesNotHaveUserProfile()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
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
            .SellerProfileService
            .UpsertMine(
                SellerProfilesTestData.CreateServiceModel(),
                CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "User can not create a SellerProfile without creating a UserProfile first.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task UpsertForUser_ShouldSucceed_ForTrustedInternalFlow_WhenProfileExists()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        var result = await testFactory
            .SellerProfileService
            .UpsertForUser(
                user.Id,
                SellerProfilesTestData.CreateServiceModel(
                    displayName: "Internal Upsert"),
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data!.UserId);
        Assert.Equal("Internal Upsert", result.Data.DisplayName);
    }

    [Fact]
    public async Task UpsertMine_ShouldReject_WhenSoftDeletedSellerProfileAlreadyExists()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "alice",
            email: "alice@example.com");

        await testFactory.CreateSellerProfile(
            user,
            displayName: "Deleted Seller",
            isDeleted: true);

        SetCurrentUser(
            currentUserService,
            user);

        var result = await testFactory
            .SellerProfileService
            .UpsertMine(
                SellerProfilesTestData.CreateServiceModel(
                    displayName: "Attempted Restore"),
                CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "User Profile was deleted. Can not beacome a seller before restoring",
            result.ErrorMessage);

        var dbModel = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        Assert.True(dbModel.IsDeleted);
        Assert.Equal("Deleted Seller", dbModel.DisplayName);
    }

    [Fact]
    public async Task All_ShouldReturnEmpty_WhenCurrentUserIsNotAdmin()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = "non-admin",
            Username = "non-admin",
            Admin = false,
        };

        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var result = await testFactory
            .SellerProfileService
            .All(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task All_ShouldReturnProfilesOrderedByCreatedOnDescending_ForAdmin()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = "admin-user",
            Username = "admin-user",
            Admin = true,
        };

        var dateTimeProvider = new FakeDateTimeProvider(
            new DateTime(2026, 03, 19, 10, 00, 00, DateTimeKind.Utc));

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (firstUser, _) = await testFactory.CreateUserWithProfile(
            username: "first",
            email: "first@example.com");

        dateTimeProvider.UtcNow = new DateTime(
            2026, 03, 19, 11, 00, 00, DateTimeKind.Utc);

        var (secondUser, _) = await testFactory.CreateUserWithProfile(
            username: "second",
            email: "second@example.com");

        dateTimeProvider.UtcNow = new DateTime(
            2026, 03, 19, 12, 00, 00, DateTimeKind.Utc);

        await testFactory.CreateSellerProfile(
            firstUser,
            displayName: "First Seller");

        dateTimeProvider.UtcNow = new DateTime(
            2026, 03, 19, 13, 00, 00, DateTimeKind.Utc);

        await testFactory.CreateSellerProfile(
            secondUser,
            displayName: "Second Seller");

        var result = await testFactory
            .SellerProfileService
            .All(CancellationToken.None);

        var profiles = result.ToList();

        Assert.Equal(2, profiles.Count);
        Assert.Equal(secondUser.Id, profiles[0].UserId);
        Assert.Equal(firstUser.Id, profiles[1].UserId);
    }

    [Fact]
    public async Task ByUserId_ShouldReturnNull_WhenCurrentUserIsNotAdmin()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = "non-admin",
            Username = "non-admin",
            Admin = false,
        };

        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var result = await testFactory
            .SellerProfileService
            .ByUserId(
                "target-user",
                CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ByUserId_ShouldReturnMatchingProfile_ForAdmin()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = "admin-user",
            Username = "admin-user",
            Admin = true,
        };

        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "seller",
            email: "seller@example.com");

        await testFactory.CreateSellerProfile(
            user,
            displayName: "Seller X");

        var result = await testFactory
            .SellerProfileService
            .ByUserId(
                user.Id,
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.UserId);
        Assert.Equal("Seller X", result.DisplayName);
    }

    [Fact]
    public async Task ChangeStatus_ShouldReject_WhenCurrentUserIsNotAdmin()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = "non-admin",
            Username = "non-admin",
            Admin = false,
        };

        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var result = await testFactory
            .SellerProfileService
            .ChangeStatus(
                "target-user",
                isActive: false,
                CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Only administrators can change seller profile status.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task ChangeStatus_ShouldUpdateIsActive_ForAdmin()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = "admin-user",
            Username = "admin-user",
            Admin = true,
        };

        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "seller",
            email: "seller@example.com");

        await testFactory.CreateSellerProfile(
            user,
            isActive: true);

        var result = await testFactory
            .SellerProfileService
            .ChangeStatus(
                user.Id,
                isActive: false,
                CancellationToken.None);

        Assert.True(result.Succeeded);

        var dbModel = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        Assert.False(dbModel.IsActive);
    }

    [Fact]
    public async Task ChangeStatus_ShouldFail_WhenTargetSellerProfileDoesNotExist()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = "admin-user",
            Username = "admin-user",
            Admin = true,
        };

        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var result = await testFactory
            .SellerProfileService
            .ChangeStatus(
                "missing-user",
                isActive: false,
                CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            string.Format(
                ErrorMessages.DbEntityNotFound,
                nameof(SellerProfileDbModel),
                "missing-user"),
            result.ErrorMessage);
    }

    [Fact]
    public async Task HasActiveProfile_ShouldReturnTrueOnlyForActiveProfile()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "seller",
            email: "seller@example.com");

        await testFactory.CreateSellerProfile(
            user,
            isActive: false);

        var inactiveResult = await testFactory
            .SellerProfileService
            .HasActiveProfile(
                user.Id,
                CancellationToken.None);

        var missingResult = await testFactory
            .SellerProfileService
            .HasActiveProfile(
                "missing-user",
                CancellationToken.None);

        Assert.False(inactiveResult);
        Assert.False(missingResult);

        var dbModel = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        dbModel.IsActive = true;
        await data.SaveChangesAsync(CancellationToken.None);

        var activeResult = await testFactory
            .SellerProfileService
            .HasActiveProfile(
                user.Id,
                CancellationToken.None);

        Assert.True(activeResult);
    }

    [Fact]
    public async Task ActiveByUserId_ShouldReturnNull_WhenProfileIsInactiveOrMissing()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "seller",
            email: "seller@example.com");

        await testFactory.CreateSellerProfile(
            user,
            isActive: false);

        var inactiveResult = await testFactory
            .SellerProfileService
            .ActiveByUserId(
                user.Id,
                CancellationToken.None);

        var missingResult = await testFactory
            .SellerProfileService
            .ActiveByUserId(
                "missing-user",
                CancellationToken.None);

        Assert.Null(inactiveResult);
        Assert.Null(missingResult);
    }

    [Fact]
    public async Task ActiveByUserId_ShouldReturnActiveSellerProfile()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var testFactory = new SellerProfilesTestFactory(
            data,
            currentUserService,
            dateTimeProvider);

        var (user, _) = await testFactory.CreateUserWithProfile(
            username: "seller",
            email: "seller@example.com");

        await testFactory.CreateSellerProfile(
            user,
            displayName: "Active Seller",
            isActive: true);

        var result = await testFactory
            .SellerProfileService
            .ActiveByUserId(
                user.Id,
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.UserId);
        Assert.True(result.IsActive);
        Assert.Equal("Active Seller", result.DisplayName);
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
