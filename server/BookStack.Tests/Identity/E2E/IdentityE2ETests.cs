namespace BookStack.Tests.Identity.E2E;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookStack.Data;
using BookStack.Features.Identity.Data.Models;
using BookStack.Features.Identity.Service.Models;
using BookStack.Features.Identity.Web.Models;
using BookStack.Features.UserProfile.Service.Models;
using BookStack.Tests.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using static BookStack.Common.Constants.Names;

public class IdentityE2ETests
{
    [Fact]
    public async Task Register_ThenAccessAuthorizedEndpoint_ShouldSucceed()
    {
        await using var factory = new IdentityApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var content = CreateRegisterContent(
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Tester");

        var registerResponse = await client.PostAsync(
            "/Identity/register/",
            content,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var jwt = await registerResponse
            .Content
            .ReadFromJsonAsync<JwtTokenServiceModel>(CancellationToken.None);

        Assert.NotNull(jwt);

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", jwt!.Token);

        var meResponse = await client.GetAsync(
            "/Profiles/mine/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var profile = await meResponse
            .Content
            .ReadFromJsonAsync<ProfileServiceModel>(CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal("Alice", profile!.FirstName);
        Assert.Equal("Tester", profile.LastName);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_ForInvalidPassword()
    {
        await using var factory = new IdentityApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            CancellationToken.None);

        var model = new LoginWebModel
        {
            Credentials = "alice",
            Password = "wrong-password"
        };

        var response = await client.PostAsJsonAsync(
            "/Identity/login/",
            model,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response
            .Content
            .ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal("Invalid login attempt!", payload!.ErrorMessage);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnGenericMessage_ForUnknownEmail()
    {
        await using var factory = new IdentityApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var model = new ForgotPasswordWebModel
        {
            Email = "missing@example.com"
        };

        var response = await client.PostAsJsonAsync(
            "/Identity/forgot-password/",
            model,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response
            .Content
            .ReadFromJsonAsync<MessageServiceModel>(CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal(
            "If an account exists for that email, a password reset link has been sent.",
            payload!.Message);

        Assert.Empty(factory.EmailSender.PasswordResetEmails);
    }

    [Fact]
    public async Task OldToken_ShouldBeRejected_AfterPasswordResetChangesSecurityStamp()
    {
        await using var factory = new IdentityApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var jwt = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            CancellationToken.None);

        string email;
        string encodedResetToken;

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope
                .ServiceProvider
                .GetRequiredService<UserManager<UserDbModel>>();

            var user = await userManager
                .FindByEmailAsync("alice@example.com");

            Assert.NotNull(user);

            email = user!.Email!;

            var rawToken = await userManager
                .GeneratePasswordResetTokenAsync(user);

            encodedResetToken = IdentityTestHelpers
                .EncodeIdentityToken(rawToken);
        }

        var model = new ResetPasswordWebModel
        {
            Email = email,
            Token = encodedResetToken,
            NewPassword = "654321"
        };

        var resetResponse = await client.PostAsJsonAsync(
            "/Identity/reset-password/",
            model,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", jwt);

        var meResponse = await client.GetAsync(
            "/Profiles/mine/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task ExistingToken_ShouldBeRejected_WhenUserIsSoftDeleted()
    {
        await using var factory = new IdentityApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var jwt = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            CancellationToken.None);

        using (var scope = factory.Services.CreateScope())
        {
            var data = scope
                .ServiceProvider
                .GetRequiredService<BookStackDbContext>();

            var user = await data
                .Users
                .IgnoreQueryFilters()
                .SingleAsync(
                    static u => u.Email == "alice@example.com",
                    CancellationToken.None);

            user.IsDeleted = true;
            await data.SaveChangesAsync(CancellationToken.None);
        }

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", jwt);

        var meResponse = await client.GetAsync(
            "/Profiles/mine/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_ShouldReturnForbidden_ForNonAdminUser()
    {
        await using var factory = new IdentityApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var jwt = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            CancellationToken.None);

        await RegisterUser(
            client,
            username: "bob",
            email: "bob@example.com",
            password: "123456",
            CancellationToken.None);

        string bobProfileId;
        using (var scope = factory.Services.CreateScope())
        {
            var data = scope
                .ServiceProvider
                .GetRequiredService<BookStackDbContext>();

            bobProfileId = await data
                .Profiles
                .Where(static p => p.User!.Email == "bob@example.com")
                .Select(static p => p.UserId)
                .SingleAsync(CancellationToken.None);
        }

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", jwt);

        var response = await client.DeleteAsync(
            $"/{AdminRoleName}/Profiles/{bobProfileId}/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_ShouldAllowAdministratorRole()
    {
        await using var factory = new IdentityApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await RegisterUser(
            client,
            username: "admin",
            email: "admin@example.com",
            password: "123456",
            CancellationToken.None);

        await RegisterUser(
            client,
            username: "bob",
            email: "bob@example.com",
            password: "123456",
            CancellationToken.None);

        string bobProfileId;
        using (var scope = factory.Services.CreateScope())
        {
            var data = scope
                .ServiceProvider
                .GetRequiredService<BookStackDbContext>();

            var userManager = scope
                .ServiceProvider
                .GetRequiredService<UserManager<UserDbModel>>();

            var roleManager = scope
                .ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var adminUser = await data
                .Users
                .SingleAsync(
                    static u => u.Email == "admin@example.com",
                    CancellationToken.None);

            bobProfileId = await data
                .Profiles
                .Where(static p => p.User!.Email == "bob@example.com")
                .Select(static p => p.UserId)
                .SingleAsync(CancellationToken.None);

            if (!await roleManager.RoleExistsAsync(AdminRoleName))
            {
                var role = new IdentityRole(AdminRoleName);
                var createRoleResult = await roleManager
                    .CreateAsync(role);

                Assert.True(
                    createRoleResult.Succeeded,
                    string.Join("; ", createRoleResult.Errors.Select(static e => e.Description)));
            }

            var addToRoleResult = await userManager.AddToRoleAsync(
                adminUser,
                AdminRoleName);

            Assert.True(
                addToRoleResult.Succeeded,
                string.Join("; ", addToRoleResult.Errors.Select(static e => e.Description)));
        }

        var model = new LoginWebModel
        {
            Credentials = "admin",
            Password = "123456"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/Identity/login/",
            model,
            CancellationToken.None);

        var jwt = await loginResponse
            .Content
            .ReadFromJsonAsync<JwtTokenServiceModel>(CancellationToken.None);

        Assert.NotNull(jwt);

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", jwt!.Token);

        var response = await client.DeleteAsync(
            $"/{AdminRoleName}/Profiles/{bobProfileId}/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<string> RegisterUser(
        HttpClient client,
        string username,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var content = CreateRegisterContent(
            username: username,
            email: email,
            password: password,
            firstName: username,
            lastName: "Tester");

        var response = await client.PostAsync(
            "/Identity/register/",
            content,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response
            .Content
            .ReadFromJsonAsync<JwtTokenServiceModel>(cancellationToken);

        Assert.NotNull(payload);

        return payload!.Token;
    }

    private static MultipartFormDataContent CreateRegisterContent(
        string username,
        string email,
        string password,
        string firstName,
        string lastName)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(username), nameof(RegisterWebModel.Username) },
            { new StringContent(email), nameof(RegisterWebModel.Email) },
            { new StringContent(password), nameof(RegisterWebModel.Password) },
            { new StringContent(firstName), nameof(RegisterWebModel.FirstName) },
            { new StringContent(lastName), nameof(RegisterWebModel.LastName) }
        };

        return content;
    }

    private sealed class ErrorResponse
    {
        public string? ErrorMessage { get; init; }
    }
}
