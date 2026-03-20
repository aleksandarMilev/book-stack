namespace BookStack.Tests.SellerProfiles.Unit;

using BookStack.Features.SellerProfiles.Service;
using BookStack.Features.SellerProfiles.Service.Models;
using BookStack.Infrastructure.Services.Result;
using BookStack.Tests.SellerProfiles;
using Microsoft.AspNetCore.Mvc;
using AdminSellerProfilesController = BookStack.Features.SellerProfiles.Web.Admin.SellerProfilesController;
using UserSellerProfilesController = BookStack.Features.SellerProfiles.Web.User.SellerProfilesController;

public class SellerProfilesUnitTests
{
    [Fact]
    public async Task Mine_ShouldReturnOk_WithPayload()
    {
        var fakeService = new FakeSellerProfileService
        {
            MineResult = new SellerProfileServiceModel
            {
                UserId = "user-1",
                DisplayName = "Alice Seller",
                PhoneNumber = "+359888123456",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
                IsActive = true,
                CreatedOn = "2026-03-19T10:00:00.0000000Z",
            }
        };

        var controller = new UserSellerProfilesController(fakeService);

        var result = await controller.Mine(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<SellerProfileServiceModel>(okResult.Value);

        Assert.Equal("user-1", payload.UserId);
        Assert.Equal("Alice Seller", payload.DisplayName);
        Assert.Equal("+359888123456", payload.PhoneNumber);
        Assert.True(payload.SupportsOnlinePayment);
        Assert.True(payload.SupportsCashOnDelivery);
        Assert.True(payload.IsActive);
    }

    [Fact]
    public async Task UpsertMine_ShouldMapInputAndReturnOk_WhenServiceSucceeds()
    {
        var fakeService = new FakeSellerProfileService
        {
            UpsertMineResult = ResultWith<SellerProfileServiceModel>.Success(
                new SellerProfileServiceModel
                {
                    UserId = "user-1",
                    DisplayName = "Alice Seller",
                    PhoneNumber = "+359888123456",
                    SupportsOnlinePayment = true,
                    SupportsCashOnDelivery = false,
                    IsActive = true,
                    CreatedOn = "2026-03-19T10:00:00.0000000Z",
                })
        };

        var controller = new UserSellerProfilesController(fakeService);
        var model = SellerProfilesTestData.CreateWebModel(
            displayName: " Alice Seller ",
            phoneNumber: " +359888123456 ",
            supportsOnlinePayment: true,
            supportsCashOnDelivery: false);

        var result = await controller.UpsertMine(
            model,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<SellerProfileServiceModel>(okResult.Value);

        Assert.Equal("user-1", payload.UserId);
        Assert.NotNull(fakeService.LastUpsertMineModel);
        Assert.Equal(" Alice Seller ", fakeService.LastUpsertMineModel!.DisplayName);
        Assert.Equal(" +359888123456 ", fakeService.LastUpsertMineModel.PhoneNumber);
        Assert.True(fakeService.LastUpsertMineModel.SupportsOnlinePayment);
        Assert.False(fakeService.LastUpsertMineModel.SupportsCashOnDelivery);
    }

    [Fact]
    public async Task UpsertMine_ShouldReturnBadRequest_WhenServiceFails()
    {
        var fakeService = new FakeSellerProfileService
        {
            UpsertMineResult = ResultWith<SellerProfileServiceModel>
                .Failure("Seller profile upsert failed.")
        };

        var controller = new UserSellerProfilesController(fakeService);

        var result = await controller.UpsertMine(
            SellerProfilesTestData.CreateWebModel(),
            CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(
            "Seller profile upsert failed.",
            GetErrorMessage(badRequestResult.Value));
    }

    [Fact]
    public async Task All_ShouldReturnOk()
    {
        var fakeService = new FakeSellerProfileService
        {
            AllResult =
            [
                new SellerProfileServiceModel
                {
                    UserId = "user-1",
                    DisplayName = "First Seller",
                    SupportsOnlinePayment = true,
                    SupportsCashOnDelivery = true,
                    IsActive = true,
                    CreatedOn = "2026-03-19T10:00:00.0000000Z",
                }
            ]
        };

        var controller = new AdminSellerProfilesController(fakeService);

        var result = await controller.All(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<SellerProfileServiceModel>>(okResult.Value);
        var profile = Assert.Single(payload);

        Assert.Equal("user-1", profile.UserId);
        Assert.Equal("First Seller", profile.DisplayName);
    }

    [Fact]
    public async Task ByUserId_ShouldReturnOk()
    {
        var fakeService = new FakeSellerProfileService
        {
            ByUserIdResult = new SellerProfileServiceModel
            {
                UserId = "user-2",
                DisplayName = "Second Seller",
                SupportsOnlinePayment = true,
                SupportsCashOnDelivery = true,
                IsActive = false,
                CreatedOn = "2026-03-19T10:00:00.0000000Z",
            }
        };

        var controller = new AdminSellerProfilesController(fakeService);

        var result = await controller.ByUserId("user-2", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<SellerProfileServiceModel>(okResult.Value);

        Assert.Equal("user-2", payload.UserId);
        Assert.Equal("Second Seller", payload.DisplayName);
        Assert.Equal("user-2", fakeService.LastByUserIdInput);
    }

    [Fact]
    public async Task ChangeStatus_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        var fakeService = new FakeSellerProfileService
        {
            ChangeStatusResult = true
        };

        var controller = new AdminSellerProfilesController(fakeService);
        var model = SellerProfilesTestData.CreateChangeStatusWebModel(isActive: false);

        var result = await controller.ChangeStatus(
            "user-1",
            model,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("user-1", fakeService.LastChangeStatusUserId);
        Assert.False(fakeService.LastChangeStatusIsActive);
    }

    [Fact]
    public async Task ChangeStatus_ShouldReturnBadRequest_WhenServiceFails()
    {
        var fakeService = new FakeSellerProfileService
        {
            ChangeStatusResult = "Seller profile status change failed."
        };

        var controller = new AdminSellerProfilesController(fakeService);

        var result = await controller.ChangeStatus(
            "missing-user",
            SellerProfilesTestData.CreateChangeStatusWebModel(),
            CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "Seller profile status change failed.",
            GetErrorMessage(badRequestResult.Value));
    }

    private static string? GetErrorMessage(object? errorObject)
        => errorObject?
            .GetType()
            .GetProperty("errorMessage")?
            .GetValue(errorObject) as string;

    private sealed class FakeSellerProfileService : ISellerProfileService
    {
        public IEnumerable<SellerProfileServiceModel> AllResult { get; init; }
            = [];

        public SellerProfileServiceModel? MineResult { get; init; }

        public SellerProfileServiceModel? ByUserIdResult { get; init; }

        public ResultWith<SellerProfileServiceModel> UpsertMineResult { get; init; }
            = ResultWith<SellerProfileServiceModel>.Failure("upsert-mine-failure");

        public ResultWith<SellerProfileServiceModel> UpsertForUserResult { get; init; }
            = ResultWith<SellerProfileServiceModel>.Failure("upsert-for-user-failure");

        public Result ChangeStatusResult { get; init; } = "change-status-failure";

        public bool HasActiveProfileResult { get; init; }

        public SellerProfileServiceModel? ActiveByUserIdResult { get; init; }

        public UpsertSellerProfileServiceModel? LastUpsertMineModel { get; private set; }

        public string? LastByUserIdInput { get; private set; }

        public string? LastChangeStatusUserId { get; private set; }

        public bool LastChangeStatusIsActive { get; private set; }

        public Task<IEnumerable<SellerProfileServiceModel>> All(
            CancellationToken cancellationToken = default)
            => Task.FromResult(this.AllResult);

        public Task<SellerProfileServiceModel?> ByUserId(
            string userId,
            CancellationToken cancellationToken = default)
        {
            this.LastByUserIdInput = userId;
            return Task.FromResult(this.ByUserIdResult);
        }

        public Task<SellerProfileServiceModel?> Mine(
            CancellationToken cancellationToken = default)
            => Task.FromResult(this.MineResult);

        public Task<ResultWith<SellerProfileServiceModel>> UpsertMine(
            UpsertSellerProfileServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.LastUpsertMineModel = model;
            return Task.FromResult(this.UpsertMineResult);
        }

        public Task<ResultWith<SellerProfileServiceModel>> UpsertForUser(
            string userId,
            UpsertSellerProfileServiceModel model,
            CancellationToken cancellationToken = default)
            => Task.FromResult(this.UpsertForUserResult);

        public Task<Result> ChangeStatus(
            string userId,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            this.LastChangeStatusUserId = userId;
            this.LastChangeStatusIsActive = isActive;
            return Task.FromResult(this.ChangeStatusResult);
        }

        public Task<bool> HasActiveProfile(
            string userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(this.HasActiveProfileResult);

        public Task<SellerProfileServiceModel?> ActiveByUserId(
            string userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(this.ActiveByUserIdResult);
    }
}
