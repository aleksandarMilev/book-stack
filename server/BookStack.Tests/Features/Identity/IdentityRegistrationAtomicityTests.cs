namespace BookStack.Tests.Features.Identity;

using BookStack.Data;
using BookStack.Features.Emails;
using BookStack.Features.Identity.Data.Models;
using BookStack.Features.Identity.Service;
using BookStack.Features.Identity.Service.Models;
using BookStack.Features.UserProfile.Service;
using BookStack.Features.UserProfile.Service.Models;
using BookStack.Infrastructure.Services.ImageValidator;
using BookStack.Infrastructure.Services.Result;
using BookStack.Infrastructure.Settings;
using BookStack.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public class IdentityRegistrationAtomicityTests
{
    [Fact]
    public async Task Register_WithoutImage_Succeeds()
    {
        await using var databaseScope = new TestDatabaseScope();
        var currentUser = new TestCurrentUserService();
        var clock = new TestDateTimeProvider(DateTime.UtcNow);
        await using var data = databaseScope.CreateDbContext(currentUser, clock);

        var profileService = new CapturingProfileService();
        var identityService = CreateIdentityService(data, profileService);
        var registration = CreateRegistrationModel("without-image");

        var result = await identityService.Register(registration, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.NotNull(profileService.LastCreateInput);
        Assert.Null(profileService.LastCreateInput!.Image);

        var users = await data
            .Set<UserDbModel>()
            .IgnoreQueryFilters()
            .Where(user => user.Email == registration.Email)
            .ToListAsync();

        var createdUser = Assert.Single(users);
        Assert.False(createdUser.IsDeleted);
    }

    [Fact]
    public async Task Register_WithValidImage_Succeeds()
    {
        await using var databaseScope = new TestDatabaseScope();
        var currentUser = new TestCurrentUserService();
        var clock = new TestDateTimeProvider(DateTime.UtcNow);
        await using var data = databaseScope.CreateDbContext(currentUser, clock);

        var profileService = new CapturingProfileService();
        var identityService = CreateIdentityService(data, profileService);
        var registration = CreateRegistrationModel(
            "with-image",
            CreateValidPngImage("avatar.png"));

        var result = await identityService.Register(registration, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.NotNull(profileService.LastCreateInput);
        Assert.NotNull(profileService.LastCreateInput!.Image);

        var users = await data
            .Set<UserDbModel>()
            .IgnoreQueryFilters()
            .Where(user => user.Email == registration.Email)
            .ToListAsync();

        var createdUser = Assert.Single(users);
        Assert.False(createdUser.IsDeleted);
    }

    [Fact]
    public async Task Register_WhenProfileCreationFails_DoesNotPersistSoftDeletedUser()
    {
        await using var databaseScope = new TestDatabaseScope();
        var currentUser = new TestCurrentUserService();
        var clock = new TestDateTimeProvider(DateTime.UtcNow);
        await using var data = databaseScope.CreateDbContext(currentUser, clock);

        var profileService = new CapturingProfileService
        {
            ThrowWhenImageProvided = true
        };

        var identityService = CreateIdentityService(data, profileService);
        var registration = CreateRegistrationModel(
            "profile-create-failure",
            CreateValidPngImage("broken.png"));

        var result = await identityService.Register(registration, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid register attempt!", result.ErrorMessage);

        var users = await data
            .Set<UserDbModel>()
            .IgnoreQueryFilters()
            .Where(user => user.Email == registration.Email)
            .ToListAsync();

        Assert.Empty(users);
    }

    private static IdentityService CreateIdentityService(
        BookStackDbContext data,
        IProfileService profileService)
    {
        var userStore = new UserStore<UserDbModel>(data);
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<UserDbModel>();
        var userValidators = new List<IUserValidator<UserDbModel>>
        {
            new UserValidator<UserDbModel>()
        };
        var passwordValidators = new List<IPasswordValidator<UserDbModel>>
        {
            new PasswordValidator<UserDbModel>()
        };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errorDescriber = new IdentityErrorDescriber();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var userManagerLogger = NullLogger<UserManager<UserDbModel>>.Instance;

        var userManager = new UserManager<UserDbModel>(
            userStore,
            identityOptions,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errorDescriber,
            serviceProvider,
            userManagerLogger);

        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "SuperStrongTestSecretKeyThatIsLongEnoughForHmac256",
            Issuer = "BookStackTests",
            Audience = "BookStackTestsClient",
        });

        var appUrlsSettings = Options.Create(new AppUrlsSettings
        {
            ClientBaseUrl = "https://bookstack.test",
        });

        return new IdentityService(
            data,
            userManager,
            new NoOpEmailSender(),
            profileService,
            NullLogger<IdentityService>.Instance,
            new ImageValidator(),
            jwtSettings,
            appUrlsSettings);
    }

    private static RegisterServiceModel CreateRegistrationModel(
        string suffix,
        IFormFile? image = null)
        => new()
        {
            Username = $"user-{suffix}",
            Email = $"user-{suffix}@bookstack.test",
            Password = "Password123!",
            FirstName = "Alice",
            LastName = "Reader",
            Image = image,
        };

    private static IFormFile CreateValidPngImage(string fileName)
    {
        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO7Z9nQAAAAASUVORK5CYII=");

        var stream = new MemoryStream(pngBytes);

        return new FormFile(stream, 0, pngBytes.Length, "image", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    private sealed class CapturingProfileService : IProfileService
    {
        public bool ThrowWhenImageProvided { get; init; }

        public CreateProfileServiceModel? LastCreateInput { get; private set; }

        public Task<ProfileServiceModel?> Mine(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProfileServiceModel?> OtherUser(string id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProfileServiceModel> Create(
            CreateProfileServiceModel model,
            string userId,
            CancellationToken cancellationToken = default)
        {
            this.LastCreateInput = model;

            if (this.ThrowWhenImageProvided && model.Image is not null)
            {
                throw new InvalidOperationException("Simulated profile image persistence failure.");
            }

            return Task.FromResult(new ProfileServiceModel
            {
                Id = userId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                ImagePath = model.Image is null
                    ? "/images/profiles/default.jpg"
                    : "/images/profiles/test-image.jpg"
            });
        }

        public Task<Result> Edit(
            CreateProfileServiceModel model,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result> Delete(
            string? userId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendWelcome(
            string email,
            string username,
            string baseUrl,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPasswordReset(
            string email,
            string resetUrl,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
