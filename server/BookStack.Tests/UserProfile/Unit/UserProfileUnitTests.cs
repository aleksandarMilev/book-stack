namespace BookStack.Tests.UserProfile.Unit;

using BookStack.Features.UserProfile.Service;
using BookStack.Features.UserProfile.Service.Models;
using BookStack.Features.UserProfile.Web.User;
using BookStack.Infrastructure.Services.Result;
using Microsoft.AspNetCore.Mvc;

public class UserProfileUnitTests
{
    [Fact]
    public async Task Mine_ShouldReturnOk_WithProfilePayload()
    {
        var fakeService = new FakeProfileService
        {
            MineResult = new ProfileServiceModel
            {
                Id = "user-1",
                FirstName = "Alice",
                LastName = "Tester",
                ImagePath = "/images/profiles/default.jpg",
            }
        };

        var controller = new ProfilesController(fakeService);

        var result = await controller.Mine(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ProfileServiceModel>(okResult.Value);

        Assert.Equal("user-1", response.Id);
        Assert.Equal("Alice", response.FirstName);
        Assert.Equal("Tester", response.LastName);
        Assert.Equal("/images/profiles/default.jpg", response.ImagePath);
    }

    [Fact]
    public async Task Edit_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        var fakeService = new FakeProfileService
        {
            EditResult = true
        };

        var controller = new ProfilesController(fakeService);
        var image = UserProfileTestData.CreateImage();
        var model = UserProfileTestData.CreateWebModel(
            firstName: "Updated",
            lastName: "Profile",
            image: image,
            removeImage: false);

        var result = await controller.Edit(
            model,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(fakeService.LastEditModel);
        Assert.Equal("Updated", fakeService.LastEditModel!.FirstName);
        Assert.Equal("Profile", fakeService.LastEditModel.LastName);
        Assert.False(fakeService.LastEditModel.RemoveImage);
        Assert.Same(image, fakeService.LastEditModel.Image);
    }

    [Fact]
    public async Task Edit_ShouldReturnBadRequest_WhenServiceFails()
    {
        var fakeService = new FakeProfileService
        {
            EditResult = "Profile edit failed."
        };

        var controller = new ProfilesController(fakeService);
        var model = UserProfileTestData.CreateWebModel();

        var result = await controller.Edit(
            model,
            CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "Profile edit failed.",
            GetErrorMessage(badRequestResult.Value));
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        var fakeService = new FakeProfileService
        {
            DeleteResult = true
        };

        var controller = new ProfilesController(fakeService);

        var result = await controller.Delete(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnBadRequest_WhenServiceFails()
    {
        var fakeService = new FakeProfileService
        {
            DeleteResult = "Profile delete failed."
        };

        var controller = new ProfilesController(fakeService);

        var result = await controller.Delete(CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "Profile delete failed.",
            GetErrorMessage(badRequestResult.Value));
    }

    private static string? GetErrorMessage(object? errorObject)
        => errorObject?
            .GetType()
            .GetProperty("errorMessage")?
            .GetValue(errorObject) as string;

    private sealed class FakeProfileService : IProfileService
    {
        public ProfileServiceModel? MineResult { get; init; }

        public Result EditResult { get; init; } = "edit-failure";

        public Result DeleteResult { get; init; } = "delete-failure";

        public CreateProfileServiceModel? LastEditModel { get; private set; }

        public Task<ProfileServiceModel?> Mine(
            CancellationToken cancellationToken = default)
            => Task.FromResult(this.MineResult);

        public Task<ProfileServiceModel> Create(
            CreateProfileServiceModel model,
            string userId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result> Edit(
            CreateProfileServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.LastEditModel = model;
            return Task.FromResult(this.EditResult);
        }

        public Task<Result> Delete(
            string? userId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(this.DeleteResult);
    }
}

