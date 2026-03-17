namespace BookStack.Tests.Features.Security;

using BookStack.Data;
using BookStack.Features.SellerProfiles.Service;
using BookStack.Features.SellerProfiles.Service.Models;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

public class SellerProfileStatusAuthorityTests
{
    [Fact]
    public async Task SellerUpsertMine_UpdatesAllowedFields_WithoutChangingActiveStatus()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = "seller-1",
            Username = "seller-1",
            Admin = false,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 11, 00, 00, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        await EnsureSellerProfile(data, "seller-1", isActive: true);

        var service = CreateService(data, currentUserService);
        var result = await service.UpsertMine(
            new UpsertSellerProfileServiceModel
            {
                DisplayName = "Updated Seller Name",
                PhoneNumber = "+359899000111",
                SupportsOnlinePayment = false,
                SupportsCashOnDelivery = true,
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);

        var profile = await data.SellerProfiles.SingleAsync(p => p.UserId == "seller-1");

        Assert.Equal("Updated Seller Name", profile.DisplayName);
        Assert.Equal("+359899000111", profile.PhoneNumber);
        Assert.False(profile.SupportsOnlinePayment);
        Assert.True(profile.SupportsCashOnDelivery);
        Assert.True(profile.IsActive);
    }

    [Fact]
    public async Task SellerUpsertMine_CannotReactivateSelf_AfterAdminDeactivation()
    {
        await using var database = new TestDatabaseScope();
        var sellerUserService = new TestCurrentUserService
        {
            UserId = "seller-1",
            Username = "seller-1",
            Admin = false,
        };

        var adminUserService = new TestCurrentUserService
        {
            UserId = "admin-1",
            Username = "admin-1",
            Admin = true,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 12, 00, 00, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            sellerUserService,
            dateTimeProvider);

        await EnsureSellerProfile(data, "seller-1", isActive: true);

        var adminService = CreateService(data, adminUserService);
        var adminDeactivateResult = await adminService.ChangeStatus(
            "seller-1",
            false,
            CancellationToken.None);

        Assert.True(adminDeactivateResult.Succeeded);

        var sellerService = CreateService(data, sellerUserService);
        var sellerUpsertResult = await sellerService.UpsertMine(
            new UpsertSellerProfileServiceModel
            {
                DisplayName = "Seller Cannot Reactivate",
                PhoneNumber = "+359899000222",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
            },
            CancellationToken.None);

        Assert.True(sellerUpsertResult.Succeeded);

        var profile = await data.SellerProfiles.SingleAsync(p => p.UserId == "seller-1");

        Assert.False(profile.IsActive);
        Assert.Equal("Seller Cannot Reactivate", profile.DisplayName);
    }

    [Fact]
    public async Task Admin_CanStillChangeSellerActiveStatus()
    {
        await using var database = new TestDatabaseScope();
        var adminUserService = new TestCurrentUserService
        {
            UserId = "admin-1",
            Username = "admin-1",
            Admin = true,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 17, 13, 00, 00, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            adminUserService,
            dateTimeProvider);

        await EnsureSellerProfile(data, "seller-1", isActive: true);

        var service = CreateService(data, adminUserService);

        var deactivateResult = await service.ChangeStatus(
            "seller-1",
            false,
            CancellationToken.None);

        Assert.True(deactivateResult.Succeeded);

        var profileAfterDeactivate = await data.SellerProfiles.SingleAsync(p => p.UserId == "seller-1");
        Assert.False(profileAfterDeactivate.IsActive);

        var reactivateResult = await service.ChangeStatus(
            "seller-1",
            true,
            CancellationToken.None);

        Assert.True(reactivateResult.Succeeded);

        var profileAfterReactivate = await data.SellerProfiles.SingleAsync(p => p.UserId == "seller-1");
        Assert.True(profileAfterReactivate.IsActive);
    }

    private static SellerProfileService CreateService(
        BookStackDbContext data,
        TestCurrentUserService currentUserService)
        => new(
            data,
            currentUserService,
            NullLogger<SellerProfileService>.Instance);

    private static async Task EnsureSellerProfile(
        BookStackDbContext data,
        string sellerId,
        bool isActive)
    {
        var userExists = await data.Users.AnyAsync(u => u.Id == sellerId);
        if (!userExists)
        {
            data.Users.Add(MarketplaceTestData.CreateUser(
                sellerId,
                $"{sellerId}@example.com"));
        }

        var profile = await data
            .SellerProfiles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(p => p.UserId == sellerId);

        if (profile is null)
        {
            profile = MarketplaceTestData.CreateSellerProfile(
                sellerId,
                isActive: isActive);

            data.SellerProfiles.Add(profile);
        }
        else
        {
            profile.IsActive = isActive;
        }

        await data.SaveChangesAsync(CancellationToken.None);
    }
}
