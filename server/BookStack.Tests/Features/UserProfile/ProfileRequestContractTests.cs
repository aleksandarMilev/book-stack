namespace BookStack.Tests.Features.UserProfile;

using BookStack.Features.UserProfile.Service;
using BookStack.Features.UserProfile.Service.Models;
using BookStack.Features.UserProfile.Web.User;
using BookStack.Features.UserProfile.Web.User.Models;
using BookStack.Infrastructure.Services.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class ProfileRequestContractTests
{
    [Fact]
    public void EditAction_UsesFromFormBindingForProfileModel()
    {
        var editMethod = typeof(ProfilesController).GetMethod(nameof(ProfilesController.Edit));
        Assert.NotNull(editMethod);

        var profileModelParameter = editMethod
            .GetParameters()
            .Single(parameter => parameter.ParameterType == typeof(CreateProfileWebModel));

        var fromFormAttribute = profileModelParameter
            .GetCustomAttributes(typeof(FromFormAttribute), inherit: false)
            .SingleOrDefault();

        Assert.NotNull(fromFormAttribute);
    }

    [Fact]
    public async Task Edit_ForwardsExpectedPayloadWithoutImage()
    {
        var profileService = new CapturingProfileService(new Result(true));
        var controller = new ProfilesController(profileService);

        var webModel = new CreateProfileWebModel
        {
            FirstName = "Alice",
            LastName = "Johnson",
            Image = null,
            RemoveImage = true
        };

        var response = await controller.Edit(
            webModel,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        Assert.NotNull(profileService.EditInput);
        Assert.Equal("Alice", profileService.EditInput!.FirstName);
        Assert.Equal("Johnson", profileService.EditInput.LastName);
        Assert.True(profileService.EditInput.RemoveImage);
        Assert.Null(profileService.EditInput.Image);
    }

    [Fact]
    public async Task Edit_ForwardsImageWhenProvided()
    {
        var profileService = new CapturingProfileService(new Result(true));
        var controller = new ProfilesController(profileService);
        var image = CreateFormFile("profile.png", "image/png", "profile-bytes");

        var webModel = new CreateProfileWebModel
        {
            FirstName = "Bob",
            LastName = "Miller",
            Image = image,
            RemoveImage = false
        };

        _ = await controller.Edit(
            webModel,
            CancellationToken.None);

        Assert.NotNull(profileService.EditInput);
        Assert.Same(image, profileService.EditInput!.Image);
    }

    private static IFormFile CreateFormFile(
        string fileName,
        string contentType,
        string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "image", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class CapturingProfileService(Result editResult) : IProfileService
    {
        public CreateProfileServiceModel? EditInput { get; private set; }

        public Task<Result> Edit(
            CreateProfileServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.EditInput = model;
            return Task.FromResult(editResult);
        }

        public Task<ProfileServiceModel?> Mine(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProfileServiceModel?> OtherUser(
            string id,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProfileServiceModel> Create(
            CreateProfileServiceModel model,
            string userId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result> Delete(
            string? userId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
