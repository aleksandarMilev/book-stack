namespace BookStack.Tests.SellerProfiles;

using BookStack.Data;
using BookStack.Features.Identity.Data.Models;
using BookStack.Features.SellerProfiles.Data.Models;
using BookStack.Features.SellerProfiles.Service;
using BookStack.Features.UserProfile.Data.Models;
using BookStack.Tests.TestInfrastructure.Factories;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

using static BookStack.Features.UserProfile.Shared.Constants;

internal sealed class SellerProfilesTestFactory
{
    private readonly BookStackDbContext data;
    private readonly FakeCurrentUserService currentUserService;
    private readonly FakeDateTimeProvider dateTimeProvider;

    public SellerProfilesTestFactory(
        BookStackDbContext data,
        FakeCurrentUserService currentUserService,
        FakeDateTimeProvider dateTimeProvider)
    {
        this.data = data;
        this.currentUserService = currentUserService;
        this.dateTimeProvider = dateTimeProvider;

        this.UserManager = TestUserManagerFactory.Create(data);
        this.SellerProfileService = this.CreateSellerProfileService();
    }

    public UserManager<UserDbModel> UserManager { get; }

    public ISellerProfileService SellerProfileService { get; }

    public async Task<UserDbModel> CreateUser(
        string username,
        string email,
        string password = "123456",
        bool isDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var user = new UserDbModel
        {
            UserName = username,
            Email = email,
            LockoutEnabled = true,
        };

        var createResult = await this
            .UserManager
            .CreateAsync(user, password);

        Assert.True(
            createResult.Succeeded,
            string.Join("; ", createResult.Errors.Select(static e => e.Description)));

        if (isDeleted)
        {
            user.IsDeleted = true;
            user.DeletedOn = this.dateTimeProvider.UtcNow;
            user.DeletedBy = this.currentUserService.GetUsername();
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var updateResult = await this
                .UserManager
                .UpdateAsync(user);

            Assert.True(
                updateResult.Succeeded,
                string.Join("; ", updateResult.Errors.Select(static e => e.Description)));
        }

        return user;
    }

    public async Task<UserProfileDbModel> CreateProfile(
        UserDbModel user,
        string firstName = "Alice",
        string lastName = "Tester",
        string imagePath = Paths.DefaultImagePath,
        bool isDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var profile = new UserProfileDbModel
        {
            UserId = user.Id,
            FirstName = firstName,
            LastName = lastName,
            ImagePath = imagePath,
            IsDeleted = isDeleted,
        };

        this.data.Profiles.Add(profile);
        await this.data.SaveChangesAsync(cancellationToken);

        return profile;
    }

    public async Task<SellerProfileDbModel> CreateSellerProfile(
        UserDbModel user,
        string displayName = "Alice Seller",
        string? phoneNumber = "+359888123456",
        bool supportsOnlinePayment = true,
        bool supportsCashOnDelivery = true,
        bool isActive = true,
        bool isDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var sellerProfile = new SellerProfileDbModel
        {
            UserId = user.Id,
            DisplayName = displayName,
            PhoneNumber = phoneNumber,
            SupportsOnlinePayment = supportsOnlinePayment,
            SupportsCashOnDelivery = supportsCashOnDelivery,
            IsActive = isActive,
            IsDeleted = isDeleted,
        };

        this.data.SellerProfiles.Add(sellerProfile);
        await this.data.SaveChangesAsync(cancellationToken);

        return sellerProfile;
    }

    public async Task<(UserDbModel User, UserProfileDbModel Profile)> CreateUserWithProfile(
        string username,
        string email,
        string password = "123456",
        string firstName = "Alice",
        string lastName = "Tester",
        string imagePath = Paths.DefaultImagePath,
        bool userDeleted = false,
        bool profileDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var user = await this.CreateUser(
            username,
            email,
            password,
            userDeleted,
            cancellationToken);

        var profile = await this.CreateProfile(
            user,
            firstName,
            lastName,
            imagePath,
            profileDeleted,
            cancellationToken);

        return (user, profile);
    }

    private ISellerProfileService CreateSellerProfileService()
        => new SellerProfileService(
            this.data,
            this.currentUserService,
            NullLogger<SellerProfileService>.Instance);
}
