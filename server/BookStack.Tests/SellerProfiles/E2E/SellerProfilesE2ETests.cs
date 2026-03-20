namespace BookStack.Tests.SellerProfiles.E2E;

using System.Net;
using System.Net.Http.Json;
using BookStack.Data;
using BookStack.Features.Identity.Data.Models;
using BookStack.Features.Identity.Service.Models;
using BookStack.Features.Identity.Web.Models;
using BookStack.Features.SellerProfiles.Service.Models;
using BookStack.Features.SellerProfiles.Web.Models;
using BookStack.Tests.SellerProfiles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using static BookStack.Common.Constants.Names;

public class SellerProfilesE2ETests
{
    [Fact]
    public async Task Register_ThenPutMine_ShouldCreateSellerProfile()
    {
        await using var factory = new SellerProfilesApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var token = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Tester",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.PutAsJsonAsync(
            "/SellerProfiles/mine/",
            SellerProfilesTestData.CreateWebModel(
                displayName: "Alice Seller",
                phoneNumber: "+359888123456",
                supportsOnlinePayment: true,
                supportsCashOnDelivery: true),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response
            .Content
            .ReadFromJsonAsync<SellerProfileServiceModel>(CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal("Alice Seller", payload!.DisplayName);
        Assert.Equal("+359888123456", payload.PhoneNumber);
        Assert.True(payload.SupportsOnlinePayment);
        Assert.True(payload.SupportsCashOnDelivery);
        Assert.True(payload.IsActive);
    }

    [Fact]
    public async Task GetMine_ShouldReturnCreatedSellerProfile()
    {
        await using var factory = new SellerProfilesApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var token = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Tester",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var upsertResponse = await client.PutAsJsonAsync(
            "/SellerProfiles/mine/",
            SellerProfilesTestData.CreateWebModel(
                displayName: "Alice Seller",
                supportsOnlinePayment: true,
                supportsCashOnDelivery: false),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);

        var mineResponse = await client.GetAsync(
            "/SellerProfiles/mine/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);

        var payload = await mineResponse
            .Content
            .ReadFromJsonAsync<SellerProfileServiceModel>(CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal("Alice Seller", payload!.DisplayName);
        Assert.True(payload.SupportsOnlinePayment);
        Assert.False(payload.SupportsCashOnDelivery);
    }

    [Fact]
    public async Task PutMine_ShouldReturnBadRequest_WhenBothPaymentMethodsAreFalse()
    {
        await using var factory = new SellerProfilesApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var token = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Tester",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.PutAsJsonAsync(
            "/SellerProfiles/mine/",
            SellerProfilesTestData.CreateWebModel(
                supportsOnlinePayment: false,
                supportsCashOnDelivery: false),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response
            .Content
            .ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal(
            "Seller profile must support at least one payment method.",
            payload!.ErrorMessage);
    }

    [Fact]
    public async Task SellerProfileCreation_ShouldRequireAuthenticatedUser()
    {
        await using var factory = new SellerProfilesApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/SellerProfiles/mine/",
            SellerProfilesTestData.CreateWebModel(),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminStatusEndpoint_ShouldReturnForbidden_ForNonAdminUser()
    {
        await using var factory = new SellerProfilesApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var nonAdminToken = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "User",
            CancellationToken.None);

        var sellerToken = await RegisterUser(
            client,
            username: "bob",
            email: "bob@example.com",
            password: "123456",
            firstName: "Bob",
            lastName: "Seller",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", sellerToken);

        var upsertResponse = await client.PutAsJsonAsync(
            "/SellerProfiles/mine/",
            SellerProfilesTestData.CreateWebModel(
                displayName: "Bob Seller"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);

        string bobUserId;
        using (var scope = factory.Services.CreateScope())
        {
            var data = scope
                .ServiceProvider
                .GetRequiredService<BookStackDbContext>();

            bobUserId = await data
                .SellerProfiles
                .Where(static p => p.User.Email == "bob@example.com")
                .Select(static p => p.UserId)
                .SingleAsync(CancellationToken.None);
        }

        client.DefaultRequestHeaders.Authorization = new("Bearer", nonAdminToken);

        var response = await client.PutAsJsonAsync(
            $"/{AdminRoleName}/SellerProfiles/{bobUserId}/status/",
            SellerProfilesTestData.CreateChangeStatusWebModel(isActive: false),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminStatusEndpoint_ShouldSucceed_ForAdminUser()
    {
        await using var factory = new SellerProfilesApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await RegisterUser(
            client,
            username: "admin",
            email: "admin@example.com",
            password: "123456",
            firstName: "Admin",
            lastName: "User",
            CancellationToken.None);

        var sellerToken = await RegisterUser(
            client,
            username: "bob",
            email: "bob@example.com",
            password: "123456",
            firstName: "Bob",
            lastName: "Seller",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", sellerToken);

        var upsertResponse = await client.PutAsJsonAsync(
            "/SellerProfiles/mine/",
            SellerProfilesTestData.CreateWebModel(
                displayName: "Bob Seller"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);

        string bobUserId;
        using (var scope = factory.Services.CreateScope())
        {
            var data = scope
                .ServiceProvider
                .GetRequiredService<BookStackDbContext>();

            bobUserId = await data
                .SellerProfiles
                .Where(static p => p.User.Email == "bob@example.com")
                .Select(static p => p.UserId)
                .SingleAsync(CancellationToken.None);
        }

        await PromoteUserToAdminRoleAsync(
            factory,
            "admin@example.com",
            CancellationToken.None);

        var adminJwt = await LoginAndGetToken(
            client,
            credentials: "admin",
            password: "123456",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", adminJwt);

        var response = await client.PutAsJsonAsync(
            $"/{AdminRoleName}/SellerProfiles/{bobUserId}/status/",
            SellerProfilesTestData.CreateChangeStatusWebModel(isActive: false),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationData = verificationScope
            .ServiceProvider
            .GetRequiredService<BookStackDbContext>();

        var sellerProfile = await verificationData
            .SellerProfiles
            .IgnoreQueryFilters()
            .SingleAsync(
                p => p.UserId == bobUserId,
                CancellationToken.None);

        Assert.False(sellerProfile.IsActive);
    }

    [Fact]
    public async Task AdminAll_ShouldReturnSellerProfiles_ForAdminUser()
    {
        await using var factory = new SellerProfilesApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await RegisterUser(
            client,
            username: "admin",
            email: "admin@example.com",
            password: "123456",
            firstName: "Admin",
            lastName: "User",
            CancellationToken.None);

        var firstSellerToken = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Seller",
            CancellationToken.None);

        var secondSellerToken = await RegisterUser(
            client,
            username: "bob",
            email: "bob@example.com",
            password: "123456",
            firstName: "Bob",
            lastName: "Seller",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", firstSellerToken);
        var firstUpsertResponse = await client.PutAsJsonAsync(
            "/SellerProfiles/mine/",
            SellerProfilesTestData.CreateWebModel(
                displayName: "Alice Seller"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, firstUpsertResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new("Bearer", secondSellerToken);
        var secondUpsertResponse = await client.PutAsJsonAsync(
            "/SellerProfiles/mine/",
            SellerProfilesTestData.CreateWebModel(
                displayName: "Bob Seller"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, secondUpsertResponse.StatusCode);

        await PromoteUserToAdminRoleAsync(
            factory,
            "admin@example.com",
            CancellationToken.None);

        var adminJwt = await LoginAndGetToken(
            client,
            credentials: "admin",
            password: "123456",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", adminJwt);

        var response = await client.GetAsync(
            $"/{AdminRoleName}/SellerProfiles/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response
            .Content
            .ReadFromJsonAsync<List<SellerProfileServiceModel>>(CancellationToken.None);

        Assert.NotNull(payload);
        Assert.True(payload!.Count >= 2);
        Assert.Contains(payload, static p => p.DisplayName == "Alice Seller");
        Assert.Contains(payload, static p => p.DisplayName == "Bob Seller");
    }

    [Fact]
    public async Task AdminByUserId_ShouldReturnSellerProfile_ForAdminUser()
    {
        await using var factory = new SellerProfilesApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await RegisterUser(
            client,
            username: "admin",
            email: "admin@example.com",
            password: "123456",
            firstName: "Admin",
            lastName: "User",
            CancellationToken.None);

        var sellerToken = await RegisterUser(
            client,
            username: "bob",
            email: "bob@example.com",
            password: "123456",
            firstName: "Bob",
            lastName: "Seller",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", sellerToken);
        var upsertResponse = await client.PutAsJsonAsync(
            "/SellerProfiles/mine/",
            SellerProfilesTestData.CreateWebModel(
                displayName: "Bob Seller"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);

        string bobUserId;
        using (var scope = factory.Services.CreateScope())
        {
            var data = scope
                .ServiceProvider
                .GetRequiredService<BookStackDbContext>();

            bobUserId = await data
                .SellerProfiles
                .Where(static p => p.User.Email == "bob@example.com")
                .Select(static p => p.UserId)
                .SingleAsync(CancellationToken.None);
        }

        await PromoteUserToAdminRoleAsync(
            factory,
            "admin@example.com",
            CancellationToken.None);

        var adminJwt = await LoginAndGetToken(
            client,
            credentials: "admin",
            password: "123456",
            CancellationToken.None);

        client.DefaultRequestHeaders.Authorization = new("Bearer", adminJwt);

        var response = await client.GetAsync(
            $"/{AdminRoleName}/SellerProfiles/{bobUserId}/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response
            .Content
            .ReadFromJsonAsync<SellerProfileServiceModel>(CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal(bobUserId, payload!.UserId);
        Assert.Equal("Bob Seller", payload.DisplayName);
    }

    [Fact]
    public async Task ExistingToken_ShouldBeRejected_WhenUserIsSoftDeleted()
    {
        await using var factory = new SellerProfilesApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var jwt = await RegisterUser(
            client,
            username: "alice",
            email: "alice@example.com",
            password: "123456",
            firstName: "Alice",
            lastName: "Tester",
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

        client.DefaultRequestHeaders.Authorization = new("Bearer", jwt);

        var response = await client.GetAsync(
            "/SellerProfiles/mine/",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
        var content = new MultipartFormDataContent
        {
            { new StringContent(username), nameof(RegisterWebModel.Username) },
            { new StringContent(email), nameof(RegisterWebModel.Email) },
            { new StringContent(password), nameof(RegisterWebModel.Password) },
            { new StringContent(firstName), nameof(RegisterWebModel.FirstName) },
            { new StringContent(lastName), nameof(RegisterWebModel.LastName) },
        };

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

    private static async Task<string> LoginAndGetToken(
        HttpClient client,
        string credentials,
        string password,
        CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync(
            "/Identity/login/",
            new LoginWebModel
            {
                Credentials = credentials,
                Password = password,
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response
            .Content
            .ReadFromJsonAsync<JwtTokenServiceModel>(cancellationToken);

        Assert.NotNull(payload);

        return payload!.Token;
    }

    private static async Task PromoteUserToAdminRoleAsync(
        SellerProfilesApiWebApplicationFactory factory,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        using var scope = factory.Services.CreateScope();

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
                u => u.Email == userEmail,
                cancellationToken);

        if (!await roleManager.RoleExistsAsync(AdminRoleName))
        {
            var role = new IdentityRole(AdminRoleName);
            var createRoleResult = await roleManager.CreateAsync(role);

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

    private sealed class ErrorResponse
    {
        public string? ErrorMessage { get; init; }
    }
}
