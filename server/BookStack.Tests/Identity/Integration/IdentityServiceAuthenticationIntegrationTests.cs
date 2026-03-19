namespace BookStack.Tests.Identity.Integration;

using System.Security.Claims;
using BookStack.Features.Identity.Data.Models;
using BookStack.Tests.Identity;
using BookStack.Tests.TestInfrastructure.DataProviders;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

using static BookStack.Common.Constants.Names;
using static BookStack.Features.Identity.Shared.Constants.ErrorMessages;
using static BookStack.Features.Identity.Shared.Constants.TokenExpiration;

public class IdentityServiceAuthenticationIntegrationTests
{
    [Fact]
    public async Task Login_ShouldSucceed_WithUsername()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();

        var utc = new DateTime(2026, 03, 19, 10, 00, 00, DateTimeKind.Utc);
        var dateTimeProvider = new FakeDateTimeProvider(utc);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        var user = await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        var loginModel = IdentityTestData.CreateLoginModel(
            credentials: "alice",
            password: "123456");

        var result = await testFactory
            .IdentityService
            .Login(
                loginModel,
                CancellationToken.None);

        Assert.True(result.Succeeded);

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

        AssertExpirationClose(
            dateTimeProvider.UtcNow.AddDays(DefaultTokenExpirationTime),
            jwt.ValidTo);
    }

    [Fact]
    public async Task Login_ShouldTrimCredentials_AndRouteToEmailLookup_WhenCredentialContainsAtSign()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        var loginModel = IdentityTestData.CreateLoginModel(
            credentials: "  alice@example.com  ",
            password: "123456");

        var result = await testFactory
            .IdentityService
            .Login(
                loginModel,
                CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Login_ShouldIncludeAdministratorRoleClaim_WhenUserIsAdmin()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out var data);

        var user = await CreateUser(
            testFactory,
            username: "admin",
            email: "admin@example.com",
            password: "123456");

        await AddUserToAdminRoleAsync(
            testFactory,
            user,
            CancellationToken.None);

        var loginModel = IdentityTestData.CreateLoginModel(
            credentials: "admin",
            password: "123456",
            rememberMe: true);

        var result = await testFactory
            .IdentityService
            .Login(
                loginModel,
                CancellationToken.None);

        Assert.True(result.Succeeded);

        var jwt = IdentityTestHelpers.ReadJwtToken(result.Data!);

        Assert.Contains(
            jwt.Claims,
            static c =>
                c.Type == ClaimTypes.Role &&
                c.Value == AdminRoleName);

        AssertExpirationClose(
            dateTimeProvider.UtcNow.AddDays(ExtendedTokenExpirationTime),
            jwt.ValidTo);
    }

    [Fact]
    public async Task Login_ShouldLockUserAfterConfiguredFailedAttempts()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        var user = await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        var firstAttempt = await testFactory
            .IdentityService
            .Login(
                IdentityTestData.CreateLoginModel(password: "wrong-1"),
                CancellationToken.None);

        var secondAttempt = await testFactory
            .IdentityService
            .Login(
                IdentityTestData.CreateLoginModel(password: "wrong-2"),
                CancellationToken.None);

        var thirdAttempt = await testFactory
            .IdentityService
            .Login(
                IdentityTestData.CreateLoginModel(password: "wrong-3"),
                CancellationToken.None);

        var fourthAttempt = await testFactory
            .IdentityService
            .Login(
                IdentityTestData.CreateLoginModel(password: "123456"),
                CancellationToken.None);

        Assert.Equal(InvalidLoginAttempt, firstAttempt.ErrorMessage);
        Assert.Equal(InvalidLoginAttempt, secondAttempt.ErrorMessage);
        Assert.Equal(AccountWasLocked, thirdAttempt.ErrorMessage);
        Assert.Equal(AccountIsLocked, fourthAttempt.ErrorMessage);

        Assert.True(await testFactory.UserManager.IsLockedOutAsync(user));
    }

    [Fact]
    public async Task Login_ShouldFail_ForDeletedUser()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        var user = await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        user.IsDeleted = true;
        await testFactory
            .UserManager
            .UpdateAsync(user);

        var loginModel = IdentityTestData.CreateLoginModel(
            password: "123456");

        var result = await testFactory
            .IdentityService
            .Login(
                loginModel,
                CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(InvalidLoginAttempt, result.ErrorMessage);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnGenericMessage_ForUnknownEmail()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        var forgotPasswordModel = IdentityTestData.CreateForgotPasswordModel(
            email: "missing@example.com");

        var result = await testFactory
            .IdentityService
            .ForgotPassword(
                forgotPasswordModel,
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            "If an account exists for that email, a password reset link has been sent.",
            result.Data);

        Assert.Empty(testFactory.EmailSender.PasswordResetEmails);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnGenericMessage_ForDeletedUser()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        var user = await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        user.IsDeleted = true;
        await testFactory
            .UserManager
            .UpdateAsync(user);

        var forgotPasswordModel = IdentityTestData.CreateForgotPasswordModel(
            email: "alice@example.com");

        var result = await testFactory
            .IdentityService
            .ForgotPassword(
                forgotPasswordModel,
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            "If an account exists for that email, a password reset link has been sent.",
            result.Data);

        Assert.Empty(testFactory.EmailSender.PasswordResetEmails);
    }

    [Fact]
    public async Task ForgotPassword_ShouldSendResetEmail_WithEncodedTokenInUrl()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        var user = await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        var forgotPasswordModel = IdentityTestData.CreateForgotPasswordModel(
            email: user.Email!);

        var result = await testFactory
            .IdentityService
            .ForgotPassword(
                forgotPasswordModel,
                CancellationToken.None);

        Assert.True(result.Succeeded);

        var email = Assert.Single(testFactory.EmailSender.PasswordResetEmails);

        Assert.Equal(user.Email, email.Email);
        Assert.StartsWith(
            "https://bookstack.test/identity/reset-password",
            email.ResetUrl,
            StringComparison.Ordinal);

        var uri = new Uri(email.ResetUrl);
        var query = QueryHelpers.ParseQuery(uri.Query);

        Assert.True(query.TryGetValue("email", out var emailValue));
        Assert.Equal("alice@example.com", emailValue.ToString());

        Assert.True(query.TryGetValue("token", out var tokenValue));
        Assert.False(string.IsNullOrWhiteSpace(tokenValue.ToString()));
    }

    [Fact]
    public async Task ForgotPassword_ShouldStillReturnGenericMessage_WhenEmailSenderThrows()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        testFactory
            .EmailSender
            .ThrowOnSendPasswordReset = true;

        await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        var forgotPasswordModel = IdentityTestData.CreateForgotPasswordModel(
            email: "alice@example.com");

        var result = await testFactory
            .IdentityService
            .ForgotPassword(
                forgotPasswordModel,
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            "If an account exists for that email, a password reset link has been sent.",
            result.Data);

        Assert.Empty(testFactory.EmailSender.PasswordResetEmails);
    }

    [Fact]
    public async Task ResetPassword_ShouldChangePassword_UpdateSecurityStamp_AndInvalidateOldPassword()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        var user = await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        var originalSecurityStamp = user.SecurityStamp;
        var rawToken = await testFactory
            .UserManager
            .GeneratePasswordResetTokenAsync(user);

        var resetPasswordModel = IdentityTestData.CreateResetPasswordModel(
            user.Email!,
            IdentityTestHelpers.EncodeIdentityToken(rawToken),
            newPassword: "654321");

        var result = await testFactory
            .IdentityService
            .ResetPassword(resetPasswordModel);

        Assert.True(result.Succeeded);
        Assert.Equal("Password successfully reset.", result.Data);

        var refreshedUser = await testFactory
            .UserManager
            .FindByIdAsync(user.Id);

        Assert.NotNull(refreshedUser);
        Assert.NotEqual(
            originalSecurityStamp,
            refreshedUser!.SecurityStamp);

        Assert.False(
            await testFactory.UserManager.CheckPasswordAsync(refreshedUser, "123456"));

        Assert.True(
            await testFactory.UserManager.CheckPasswordAsync(refreshedUser, "654321"));
    }

    [Fact]
    public async Task ResetPassword_ShouldFail_ForInvalidEncodedToken()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        var user = await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        var resetPasswordModel = IdentityTestData.CreateResetPasswordModel(
            user.Email!,
            "not-base64-url");

        var result = await testFactory
            .IdentityService
            .ResetPassword(resetPasswordModel);

        Assert.False(result.Succeeded);
        Assert.Equal(
            InvalidPasswordResetAttempt,
            result.ErrorMessage);

        Assert.True(
            await testFactory.UserManager.CheckPasswordAsync(user, "123456"));
    }

    [Fact]
    public async Task ResetPassword_ShouldFail_ForDeletedUser()
    {
        await using var database = new SqliteTestDatabase();

        var currentUserService = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        var testFactory = database.CreateIdentityFactory(
            currentUserService,
            dateTimeProvider,
            out _);

        var user = await CreateUser(
            testFactory,
            username: "alice",
            email: "alice@example.com",
            password: "123456");

        user.IsDeleted = true;
        await testFactory
            .UserManager
            .UpdateAsync(user);

        var rawToken = await testFactory
            .UserManager
            .GeneratePasswordResetTokenAsync(user);

        var resetPasswordModel = IdentityTestData.CreateResetPasswordModel(
            user.Email!,
            IdentityTestHelpers.EncodeIdentityToken(rawToken));

        var result = await testFactory
            .IdentityService
            .ResetPassword(resetPasswordModel);

        Assert.False(result.Succeeded);
        Assert.Equal(
            InvalidPasswordResetAttempt,
            result.ErrorMessage);
    }

    private static async Task<UserDbModel> CreateUser(
        IdentityTestFactory testFactory,
        string username,
        string email,
        string password)
    {
        var user = new UserDbModel
        {
            UserName = username,
            Email = email,
            LockoutEnabled = true,
        };

        var result = await testFactory
            .UserManager
            .CreateAsync(user, password);

        Assert.True(
            result.Succeeded,
            string.Join("; ", result.Errors.Select(static e => e.Description)));

        return user;
    }

    private static void AssertExpirationClose(
        DateTime expectedUtc,
        DateTime actualUtc)
        => Assert.InRange(
            actualUtc,
            expectedUtc.AddSeconds(-1),
            expectedUtc.AddSeconds(1));

    private static async Task AddUserToAdminRoleAsync(
        IdentityTestFactory testFactory,
        UserDbModel user,
        CancellationToken cancellationToken = default)
    {
        var roleExists = await testFactory
            .UserManager
            .IsInRoleAsync(user, AdminRoleName);

        if (roleExists)
        {
            return;
        }

        var roleStore = testFactory
            .Data
            .Set<IdentityRole>();

        var existingRole = await roleStore
            .SingleOrDefaultAsync(
                static r => r.Name == AdminRoleName,
                cancellationToken);

        if (existingRole is null)
        {
            var role = new IdentityRole
            {
                Name = AdminRoleName,
                NormalizedName = AdminRoleName.ToUpperInvariant()
            };

            roleStore.Add(role);

            await testFactory
                .Data
                .SaveChangesAsync(cancellationToken);
        }

        var addToRoleResult = await testFactory
            .UserManager
            .AddToRoleAsync(user, AdminRoleName);

        Assert.True(
            addToRoleResult.Succeeded,
            string.Join("; ", addToRoleResult.Errors.Select(static e => e.Description)));
    }
}
