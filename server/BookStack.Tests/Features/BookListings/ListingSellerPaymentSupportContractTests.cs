namespace BookStack.Tests.Features.BookListings;

using BookStack.Features.BookListings.Service.Models;
using BookStack.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

public class ListingSellerPaymentSupportContractTests
{
    [Fact]
    public async Task ListingContracts_ReturnCorrectSellerPaymentSupportFlags()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = null,
            Username = null,
            Admin = false,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 18, 8, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        var bothMethodsListingId = await SeedApprovedListing(
            data,
            sellerId: "seller-both",
            supportsOnlinePayment: true,
            supportsCashOnDelivery: true);

        var onlineOnlyListingId = await SeedApprovedListing(
            data,
            sellerId: "seller-online",
            supportsOnlinePayment: true,
            supportsCashOnDelivery: false);

        var codOnlyListingId = await SeedApprovedListing(
            data,
            sellerId: "seller-cod",
            supportsOnlinePayment: false,
            supportsCashOnDelivery: true);

        var listResult = await service.All(
            new BookListingFilterServiceModel
            {
                IsApproved = true,
                PageIndex = 1,
                PageSize = 50,
            },
            CancellationToken.None);

        var bothMethodsListing = listResult.Items.Single(i => i.Id == bothMethodsListingId);
        Assert.True(bothMethodsListing.SupportsOnlinePayment);
        Assert.True(bothMethodsListing.SupportsCashOnDelivery);

        var onlineOnlyListing = listResult.Items.Single(i => i.Id == onlineOnlyListingId);
        Assert.True(onlineOnlyListing.SupportsOnlinePayment);
        Assert.False(onlineOnlyListing.SupportsCashOnDelivery);

        var codOnlyListing = listResult.Items.Single(i => i.Id == codOnlyListingId);
        Assert.False(codOnlyListing.SupportsOnlinePayment);
        Assert.True(codOnlyListing.SupportsCashOnDelivery);

        var bothDetails = await service.Details(bothMethodsListingId, CancellationToken.None);
        Assert.NotNull(bothDetails);
        Assert.True(bothDetails!.SupportsOnlinePayment);
        Assert.True(bothDetails.SupportsCashOnDelivery);

        var onlineOnlyDetails = await service.Details(onlineOnlyListingId, CancellationToken.None);
        Assert.NotNull(onlineOnlyDetails);
        Assert.True(onlineOnlyDetails!.SupportsOnlinePayment);
        Assert.False(onlineOnlyDetails.SupportsCashOnDelivery);

        var codOnlyDetails = await service.Details(codOnlyListingId, CancellationToken.None);
        Assert.NotNull(codOnlyDetails);
        Assert.False(codOnlyDetails!.SupportsOnlinePayment);
        Assert.True(codOnlyDetails.SupportsCashOnDelivery);
    }

    [Fact]
    public async Task ListingContracts_RespectActiveSellerProfileConstraint_ForPaymentSupportFlags()
    {
        await using var database = new TestDatabaseScope();
        var currentUserService = new TestCurrentUserService
        {
            UserId = null,
            Username = null,
            Admin = false,
        };

        var dateTimeProvider = new TestDateTimeProvider(
            new DateTime(2026, 03, 18, 9, 0, 0, DateTimeKind.Utc));

        await using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var service = TestServiceFactory.CreateBookListingService(
            data,
            currentUserService,
            dateTimeProvider,
            new FakeImageWriter());

        var inactiveSellerListingId = await SeedApprovedListing(
            data,
            sellerId: "seller-inactive",
            supportsOnlinePayment: true,
            supportsCashOnDelivery: true);

        var sellerProfile = await data
            .SellerProfiles
            .SingleAsync(p => p.UserId == "seller-inactive");

        sellerProfile.IsActive = false;
        await data.SaveChangesAsync(CancellationToken.None);

        var listResult = await service.All(
            new BookListingFilterServiceModel
            {
                IsApproved = true,
                PageIndex = 1,
                PageSize = 10,
            },
            CancellationToken.None);

        var listModel = listResult.Items.Single(i => i.Id == inactiveSellerListingId);
        Assert.False(listModel.SupportsOnlinePayment);
        Assert.False(listModel.SupportsCashOnDelivery);

        var detailsModel = await service.Details(inactiveSellerListingId, CancellationToken.None);
        Assert.NotNull(detailsModel);
        Assert.False(detailsModel!.SupportsOnlinePayment);
        Assert.False(detailsModel.SupportsCashOnDelivery);
    }

    private static async Task<Guid> SeedApprovedListing(
        BookStack.Data.BookStackDbContext data,
        string sellerId,
        bool supportsOnlinePayment,
        bool supportsCashOnDelivery)
    {
        await EnsureSellerProfile(
            data,
            sellerId,
            supportsOnlinePayment,
            supportsCashOnDelivery);

        var book = MarketplaceTestData.CreateApprovedBook(
            creatorId: sellerId,
            title: $"Listing Contract Book {sellerId}",
            author: "Listing Contract Author");

        data.Books.Add(book);
        await data.SaveChangesAsync(CancellationToken.None);

        var listing = MarketplaceTestData.CreateApprovedListing(
            book.Id,
            creatorId: sellerId,
            currency: "EUR");

        data.BookListings.Add(listing);
        await data.SaveChangesAsync(CancellationToken.None);

        return listing.Id;
    }

    private static async Task EnsureSellerProfile(
        BookStack.Data.BookStackDbContext data,
        string sellerId,
        bool supportsOnlinePayment,
        bool supportsCashOnDelivery)
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
                isActive: true,
                supportsOnlinePayment: supportsOnlinePayment,
                supportsCashOnDelivery: supportsCashOnDelivery);

            data.SellerProfiles.Add(profile);
        }
        else
        {
            profile.IsActive = true;
            profile.SupportsOnlinePayment = supportsOnlinePayment;
            profile.SupportsCashOnDelivery = supportsCashOnDelivery;
        }

        await data.SaveChangesAsync(CancellationToken.None);
    }
}
