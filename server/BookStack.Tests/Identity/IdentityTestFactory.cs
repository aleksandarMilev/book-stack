namespace BookStack.Tests.Identity;

using BookStack.Data;
using BookStack.Features.Identity.Data.Models;
using BookStack.Features.Identity.Service;
using BookStack.Features.UserProfile.Service;
using BookStack.Infrastructure.Services.ImageWriter;
using BookStack.Infrastructure.Services.StringSanitizer;
using BookStack.Infrastructure.Settings;
using BookStack.Tests.TestInfrastructure.Factories;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

internal sealed class IdentityTestFactory
{
    private readonly BookStackDbContext data;
    private readonly FakeCurrentUserService currentUser;
    private readonly FakeDateTimeProvider dateTimeProvider;

    public IdentityTestFactory(
        BookStackDbContext data,
        FakeCurrentUserService currentUser,
        FakeDateTimeProvider dateTimeProvider)
    {
        this.data = data;
        this.currentUser = currentUser;
        this.dateTimeProvider = dateTimeProvider;

        this.EmailSender = new FakeEmailSender();
        this.ImageWriter = new FakeImageWriter();
        this.UserManager = TestUserManagerFactory.Create(data);
        this.StringSanitizer = new StringSanitizerService();
        this.ProfileService = this.CreateProfileService();
        this.IdentityService = this.CreateIdentityService();
    }

    public BookStackDbContext Data
        => this.data;

    public FakeEmailSender EmailSender { get; }

    public IImageWriter ImageWriter { get; }

    public IStringSanitizerService StringSanitizer { get; }

    public UserManager<UserDbModel> UserManager { get; }

    public IProfileService ProfileService { get; }

    public IIdentityService IdentityService { get; }

    private ProfileService CreateProfileService()
        => new(
            this.data,
            this.UserManager,
            this.currentUser,
            this.ImageWriter,
            this.StringSanitizer,
            NullLogger<ProfileService>.Instance);

    private IdentityService CreateIdentityService()
    {
        var jwtSettings = new JwtSettings
        {
            Secret = "super_secret_test_key_12345678901234567890",
            Issuer = "BookStack.Tests",
            Audience = "BookStack.Tests.Client",
        };

        var jwtSettingsOptions = Options.Create(jwtSettings);

        var appUrlsSettings = new AppUrlsSettings
        {
            ClientBaseUrl = "https://bookstack.test",
        };

        var appUrlsSettingsOptions = Options.Create(appUrlsSettings);

        return new IdentityService(
            this.data,
            this.UserManager,
            this.EmailSender,
            this.ProfileService,
            NullLogger<IdentityService>.Instance,
            jwtSettingsOptions,
            this.dateTimeProvider,
            appUrlsSettingsOptions);
    }
}