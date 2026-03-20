namespace BookStack.Tests.UserProfile;

using BookStack.Data;
using BookStack.Features.Identity.Data.Models;
using BookStack.Features.UserProfile.Data.Models;
using BookStack.Features.UserProfile.Service;
using BookStack.Infrastructure.Services.StringSanitizer;
using BookStack.Tests.TestInfrastructure.Factories;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

using static BookStack.Features.UserProfile.Shared.Constants;

internal sealed class UserProfileTestFactory
{
    private readonly BookStackDbContext data;
    private readonly FakeCurrentUserService currentUserService;
    private readonly FakeDateTimeProvider dateTimeProvider;

    public UserProfileTestFactory(
        BookStackDbContext data,
        FakeCurrentUserService currentUserService,
        FakeDateTimeProvider dateTimeProvider)
    {
        this.data = data;
        this.currentUserService = currentUserService;
        this.dateTimeProvider = dateTimeProvider;

        this.ImageWriter = new FakeImageWriter();
        this.UserManager = TestUserManagerFactory.Create(data);
        this.StringSanitizer = new StringSanitizerService();
        this.ProfileService = this.CreateProfileService();
    }

    public BookStackDbContext Data
        => this.data;

    public FakeImageWriter ImageWriter { get; }

    public IStringSanitizerService StringSanitizer { get; }

    public UserManager<UserDbModel> UserManager { get; }

    public IProfileService ProfileService { get; }

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

    private ProfileService CreateProfileService()
        => new(
            this.data,
            this.UserManager,
            this.currentUserService,
            this.dateTimeProvider,
            this.ImageWriter,
            this.StringSanitizer,
            NullLogger<ProfileService>.Instance);
}

