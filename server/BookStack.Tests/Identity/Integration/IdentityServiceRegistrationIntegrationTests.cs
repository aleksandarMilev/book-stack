namespace BookStack.Tests.Identity.Integration;

using System.Security.Claims;
using System.Text.Json;
using BookStack.Data;
using BookStack.Features.Identity.Data.Models;
using BookStack.Features.Identity.Outbox;
using BookStack.Features.Identity.Service;
using BookStack.Features.UserProfile.Service;
using BookStack.Features.UserProfile.Service.Models;
using BookStack.Infrastructure.Services.Result;
using BookStack.Infrastructure.Settings;
using BookStack.Tests.Identity;
using BookStack.Tests.TestInfrastructure.DataProviders;
using BookStack.Tests.TestInfrastructure.Factories;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using static BookStack.Common.Constants.Names;
using static BookStack.Features.Identity.Shared.Constants.ErrorMessages;
using static BookStack.Infrastructure.Outbox.Common.Constants.MessageTypes;

public class IdentityServiceRegistrationIntegrationTests
{
    [Fact]
    public async Task Register_ShouldCreateUserProfileAndOutboxMessage_WhenInputIsValid()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();

        var utc = new DateTime(2026, 03, 19, 10, 00, 00, DateTimeKind.Utc);
        var dateTimeProvider = new FakeDateTimeProvider(utc);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out var data);

        var result = await testFactory
            .IdentityService
            .Register(
                IdentityTestData.CreateRegisterModel(),
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);

        var user = Assert.Single(data.Users);
        Assert.Equal("alice", user.UserName);
        Assert.Equal("alice@example.com", user.Email);
        Assert.False(user.IsDeleted);

        var profile = Assert.Single(data.Profiles);
        Assert.Equal(user.Id, profile.UserId);
        Assert.Equal("Alice", profile.FirstName);
        Assert.Equal("Tester", profile.LastName);

        var outboxMessage = Assert.Single(data.OutboxMessages);
        Assert.Equal(IdentityWelcomeEmailRequested, outboxMessage.Type);
        Assert.Equal(dateTimeProvider.UtcNow, outboxMessage.OccurredOnUtc);
        Assert.Equal(dateTimeProvider.UtcNow, outboxMessage.CreatedOnUtc);
        Assert.Equal(dateTimeProvider.UtcNow, outboxMessage.NextAttemptOnUtc);

        var payload = JsonSerializer
            .Deserialize<WelcomeEmailOutboxPayload>(outboxMessage.PayloadJson);

        Assert.NotNull(payload);
        Assert.Equal(user.Id, payload!.UserId);
        Assert.Equal("alice@example.com", payload.Email);
        Assert.Equal("alice", payload.Username);
        Assert.Equal("https://bookstack.test", payload.BaseUrl);

        var jwt = IdentityTestHelpers.ReadJwtToken(result.Data!);
        Assert.Equal(
            user.Id,
            IdentityTestHelpers.GetClaimValue(jwt, ClaimTypes.NameIdentifier));

        Assert.Equal(
            "alice",
            IdentityTestHelpers.GetClaimValue(jwt, ClaimTypes.Name));

        Assert.Equal(
            "alice@example.com",
            IdentityTestHelpers.GetClaimValue(jwt, ClaimTypes.Email));

        Assert.Equal(
            user.SecurityStamp,
            IdentityTestHelpers.GetClaimValue(jwt, "security_stamp"));

        Assert.DoesNotContain(
            jwt.Claims,
            static c =>
                c.Type == ClaimTypes.Role &&
                c.Value == AdminRoleName);
    }

    [Fact]
    public async Task Register_ShouldRejectUsernameThatLooksLikeEmail()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out var data);

        var model = IdentityTestData.CreateRegisterModel(
            username: "alice@example.com");

        var result = await testFactory
            .IdentityService
            .Register(model, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Username cannot be in email format.",
            result.ErrorMessage);

        Assert.Empty(data.Users);
        Assert.Empty(data.Profiles);
        Assert.Empty(data.OutboxMessages);
    }

    [Fact]
    public async Task Register_ShouldRejectDuplicateUsername_WhenExistingUserIsSoftDeleted()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out var data);

        var existingUser = new UserDbModel
        {
            UserName = "alice",
            Email = "deleted@example.com",
            IsDeleted = true,
            LockoutEnabled = true,
        };

        var createResult = await testFactory
            .UserManager
            .CreateAsync(existingUser, "123456");

        Assert.True(createResult.Succeeded);

        var model = IdentityTestData.CreateRegisterModel(
            username: "alice",
            email: "alice@example.com");

        var result = await testFactory
            .IdentityService
            .Register(model, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Username 'alice' is already taken.",
            result.ErrorMessage);

        Assert.Single(data.Users.IgnoreQueryFilters());
        Assert.Empty(data.Profiles);
        Assert.Empty(data.OutboxMessages);
    }

    [Fact]
    public async Task Register_ShouldRejectDuplicateEmail_WhenExistingUserIsSoftDeleted()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out var data);

        var existingUser = new UserDbModel
        {
            UserName = "deleted-user",
            Email = "alice@example.com",
            IsDeleted = true,
            LockoutEnabled = true,
        };

        var createResult = await testFactory
            .UserManager
            .CreateAsync(existingUser, "123456");

        Assert.True(createResult.Succeeded);

        var model = IdentityTestData.CreateRegisterModel(
            username: "alice",
            email: "alice@example.com");

        var result = await testFactory
            .IdentityService
            .Register(model, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Email 'alice@example.com' is already taken.",
            result.ErrorMessage);

        Assert.Single(data.Users.IgnoreQueryFilters());
        Assert.Empty(data.Profiles);
        Assert.Empty(data.OutboxMessages);
    }

    [Fact]
    public async Task Register_ShouldRollbackAllWrites_WhenProfileCreationFails()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        using var data = database.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        var userManager = TestUserManagerFactory.Create(data);

        var service = CreateIdentityService(
            data,
            userManager,
            new ThrowingProfileService(),
            new FakeEmailSender(),
            dateTimeProvider);

        var result = await service.Register(
            IdentityTestData.CreateRegisterModel(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(InvalidRegisterAttempt, result.ErrorMessage);
        Assert.Empty(data.Users.IgnoreQueryFilters());
        Assert.Empty(data.Profiles.IgnoreQueryFilters());
        Assert.Empty(data.OutboxMessages);
    }

    private static IdentityService CreateIdentityService(
        BookStackDbContext data,
        UserManager<UserDbModel> userManager,
        IProfileService profileService,
        FakeEmailSender emailSender,
        FakeDateTimeProvider dateTimeProvider)
        => new(
            data,
            userManager,
            emailSender,
            profileService,
            NullLogger<IdentityService>.Instance,
            Options.Create(new JwtSettings
            {
                Secret = "super_secret_test_key_12345678901234567890",
                Issuer = "BookStack.Tests",
                Audience = "BookStack.Tests.Client",
            }),
            dateTimeProvider,
            Options.Create(new AppUrlsSettings
            {
                ClientBaseUrl = "https://bookstack.test"
            }));

    private sealed class ThrowingProfileService : IProfileService
    {
        public Task<ProfileServiceModel?> Mine(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProfileServiceModel?> OtherUser(
            string userId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProfileServiceModel> Create(
            CreateProfileServiceModel serviceModel,
            string userId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Profile creation failed.");

        public Task<Result> Edit(
            CreateProfileServiceModel serviceModel,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result> Delete(
            string? userToDeleteId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
