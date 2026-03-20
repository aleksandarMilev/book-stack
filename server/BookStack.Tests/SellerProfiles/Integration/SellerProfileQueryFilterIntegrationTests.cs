namespace BookStack.Tests.SellerProfiles.Integration;

using BookStack.Features.Identity.Data.Models;
using BookStack.Features.SellerProfiles.Data.Models;
using BookStack.Tests.TestInfrastructure.DataProviders;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.EntityFrameworkCore;

public class SellerProfileQueryFilterIntegrationTests
{
    [Fact]
    public async Task SellerProfileQueryFilter_ShouldHideSoftDeletedSellerProfiles()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var activeUser = CreateUser(
            "active-seller",
            "active-seller@example.com");

        var deletedSellerUser = CreateUser(
            "deleted-seller",
            "deleted-seller@example.com");

        data.Users.AddRange(activeUser, deletedSellerUser);
        data.SellerProfiles.AddRange(
            new SellerProfileDbModel
            {
                UserId = activeUser.Id,
                DisplayName = "Active Seller",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
                IsActive = true,
            },
            new SellerProfileDbModel
            {
                UserId = deletedSellerUser.Id,
                DisplayName = "Soft Deleted Seller",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
                IsActive = true,
                IsDeleted = true,
            });

        await data.SaveChangesAsync(CancellationToken.None);

        var visibleProfilesCount = await data
            .SellerProfiles
            .CountAsync(CancellationToken.None);

        var allProfilesCount = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .CountAsync(CancellationToken.None);

        Assert.Equal(1, visibleProfilesCount);
        Assert.Equal(2, allProfilesCount);
    }

    [Fact]
    public async Task SellerProfileQueryFilter_ShouldHideSellerProfilesOfSoftDeletedUsers()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var activeUser = CreateUser(
            "active-seller",
            "active-seller@example.com");

        var deletedUser = CreateUser(
            "deleted-user",
            "deleted-user@example.com",
            isDeleted: true);

        data.Users.AddRange(activeUser, deletedUser);
        data.SellerProfiles.AddRange(
            new SellerProfileDbModel
            {
                UserId = activeUser.Id,
                DisplayName = "Active Seller",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
                IsActive = true,
            },
            new SellerProfileDbModel
            {
                UserId = deletedUser.Id,
                DisplayName = "Deleted User Seller",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
                IsActive = true,
            });

        await data.SaveChangesAsync(CancellationToken.None);

        var visibleProfilesCount = await data
            .SellerProfiles
            .CountAsync(CancellationToken.None);

        var allProfilesCount = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .CountAsync(CancellationToken.None);

        Assert.Equal(1, visibleProfilesCount);
        Assert.Equal(2, allProfilesCount);
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldExposeSoftDeletedSellerProfilesAndDeletedUsersRows()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var activeUser = CreateUser(
            "active-seller",
            "active-seller@example.com");

        var deletedSellerUser = CreateUser(
            "deleted-seller",
            "deleted-seller@example.com");

        var deletedUser = CreateUser(
            "deleted-user",
            "deleted-user@example.com",
            isDeleted: true);

        data.Users.AddRange(activeUser, deletedSellerUser, deletedUser);
        data.SellerProfiles.AddRange(
            new SellerProfileDbModel
            {
                UserId = activeUser.Id,
                DisplayName = "Active Seller",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
                IsActive = true,
            },
            new SellerProfileDbModel
            {
                UserId = deletedSellerUser.Id,
                DisplayName = "Soft Deleted Seller",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
                IsActive = true,
                IsDeleted = true,
            },
            new SellerProfileDbModel
            {
                UserId = deletedUser.Id,
                DisplayName = "Deleted User Seller",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
                IsActive = true,
            });

        await data.SaveChangesAsync(CancellationToken.None);

        var visibleProfilesCount = await data
            .SellerProfiles
            .CountAsync(CancellationToken.None);

        var allProfilesCount = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .CountAsync(CancellationToken.None);

        Assert.Equal(1, visibleProfilesCount);
        Assert.Equal(3, allProfilesCount);
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
