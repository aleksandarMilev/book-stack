namespace BookStack.Tests.UserProfile.E2E;

using System.Net;
using System.Net.Http.Json;
using BookStack.Data;
using BookStack.Features.Identity.Data.Models;
using BookStack.Features.Identity.Service.Models;
using BookStack.Features.Identity.Web.Models;
using BookStack.Features.UserProfile.Service.Models;
using BookStack.Features.UserProfile.Web.User.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using static BookStack.Common.Constants.Names;

public class UserProfileE2ETests
{
    [Fact]
    public async Task Register_ThenMine_ShouldReturnCreatedProfile()
    {
        await using var factory = new UserProfileApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var token = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Tester",
            CancellationToken.None);

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", token);

        var response = await client.GetAsync(
            "/Profiles/mine/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response
            .Content
            .ReadFromJsonAsync<ProfileServiceModel>(CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal("Alice", profile!.FirstName);
        Assert.Equal("Tester", profile.LastName);
    }

    [Fact]
    public async Task PutProfiles_ShouldUpdateFirstAndLastName_ForAuthenticatedUser()
    {
        await using var factory = new UserProfileApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var token = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Tester",
            CancellationToken.None);

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", token);

        var editContent = CreateEditContent(
            firstName: "Updated",
            lastName: "Profile");

        var editResponse = await client.PutAsync(
            "/Profiles/",
            editContent,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, editResponse.StatusCode);

        var mineResponse = await client.GetAsync(
            "/Profiles/mine/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);

        var profile = await mineResponse
            .Content
            .ReadFromJsonAsync<ProfileServiceModel>(CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal("Updated", profile!.FirstName);
        Assert.Equal("Profile", profile.LastName);
    }

    [Fact]
    public async Task DeleteProfiles_ShouldSoftDeleteProfileAndUser_ForAuthenticatedUser()
    {
        await using var factory = new UserProfileApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var token = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Tester",
            CancellationToken.None);

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", token);

        var deleteResponse = await client.DeleteAsync(
            "/Profiles/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var scope = factory.Services.CreateScope();

        var data = scope
            .ServiceProvider
            .GetRequiredService<BookStackDbContext>();

        var user = await data
            .Users
            .IgnoreQueryFilters()
            .SingleAsync(
                static u => u.Email == "alice@example.com",
                CancellationToken.None);

        var profile = await data
            .Profiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == user.Id,
                CancellationToken.None);

        Assert.True(profile.IsDeleted);
        Assert.True(user.IsDeleted);
    }

    [Fact]
    public async Task OldToken_ShouldBeRejected_AfterSelfDelete()
    {
        await using var factory = new UserProfileApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var token = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Tester",
            CancellationToken.None);

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", token);

        var deleteResponse = await client.DeleteAsync(
            "/Profiles/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var mineResponse = await client.GetAsync(
            "/Profiles/mine/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, mineResponse.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_ShouldAllowAdministratorRole_ToDeleteAnotherUser()
    {
        await using var factory = new UserProfileApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await RegisterUser(
            client,
            username: "admin",
            email: "admin@example.com",
            password: "123456",
            firstName: "Admin",
            lastName: "User",
            CancellationToken.None);

        await RegisterUser(
            client,
            username: "bob",
            email: "bob@example.com",
            password: "123456",
            firstName: "Bob",
            lastName: "User",
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

            var addToRoleResult = await userManager
                .AddToRoleAsync(adminUser, AdminRoleName);

            Assert.True(
                addToRoleResult.Succeeded,
                string.Join("; ", addToRoleResult.Errors.Select(static e => e.Description)));
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/Identity/login/",
            new LoginWebModel
            {
                Credentials = "admin",
                Password = "123456",
            },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginPayload = await loginResponse
            .Content
            .ReadFromJsonAsync<JwtTokenServiceModel>(CancellationToken.None);

        Assert.NotNull(loginPayload);

        client
            .DefaultRequestHeaders
            .Authorization = new("Bearer", loginPayload!.Token);

        var deleteResponse = await client.DeleteAsync(
            $"/{AdminRoleName}/Profiles/{bobProfileId}/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var verificationScope = factory.Services.CreateScope();

        var verificationData = verificationScope
            .ServiceProvider
            .GetRequiredService<BookStackDbContext>();

        var bobUser = await verificationData
            .Users
            .IgnoreQueryFilters()
            .SingleAsync(
                static u => u.Email == "bob@example.com",
                CancellationToken.None);

        var bobProfile = await verificationData
            .Profiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == bobUser.Id,
                CancellationToken.None);

        Assert.True(bobUser.IsDeleted);
        Assert.True(bobProfile.IsDeleted);
    }

    [Fact]
    public async Task AdminEndpoint_ShouldReturnForbidden_ForNonAdminUser()
    {
        await using var factory = new UserProfileApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var nonAdminToken = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "User",
            CancellationToken.None);

        await RegisterUser(
            client,
            username: "bob",
            email: "bob@example.com",
            password: "123456",
            firstName: "Bob",
            lastName: "User",
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
            .Authorization = new("Bearer", nonAdminToken);

        var response = await client.DeleteAsync(
            $"/{AdminRoleName}/Profiles/{bobProfileId}/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<string> RegisterUser(
        HttpClient client,
        string username,
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var content = CreateRegisterContent(
            username,
            email,
            password,
            firstName,
            lastName);

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
        => new()
        {
            { new StringContent(username), nameof(RegisterWebModel.Username) },
            { new StringContent(email), nameof(RegisterWebModel.Email) },
            { new StringContent(password), nameof(RegisterWebModel.Password) },
            { new StringContent(firstName), nameof(RegisterWebModel.FirstName) },
            { new StringContent(lastName), nameof(RegisterWebModel.LastName) },
        };

    private static MultipartFormDataContent CreateEditContent(
        string firstName,
        string lastName,
        bool removeImage = false)
        => new()
        {
            { new StringContent(firstName), nameof(CreateProfileWebModel.FirstName) },
            { new StringContent(lastName), nameof(CreateProfileWebModel.LastName) },
            { new StringContent(removeImage.ToString()), nameof(CreateProfileWebModel.RemoveImage) },
        };
}

