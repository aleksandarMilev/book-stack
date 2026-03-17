namespace BookStack.Tests.Features.Identity;

using BookStack.Features.Identity.Service;
using BookStack.Features.Identity.Service.Models;
using BookStack.Features.Identity.Web;
using BookStack.Features.Identity.Web.Models;
using BookStack.Infrastructure.Services.Result;
using Microsoft.AspNetCore.Mvc;

public class RegistrationContractTests
{
    [Fact]
    public void RegisterAction_UsesFromFormBindingForRegistrationModel()
    {
        var registerMethod = typeof(IdentityController).GetMethod(nameof(IdentityController.Register));
        Assert.NotNull(registerMethod);

        var registerModelParameter = registerMethod
            .GetParameters()
            .Single(parameter => parameter.ParameterType == typeof(RegisterWebModel));

        var fromFormAttribute = registerModelParameter
            .GetCustomAttributes(typeof(FromFormAttribute), inherit: false)
            .SingleOrDefault();

        Assert.NotNull(fromFormAttribute);
    }

    [Fact]
    public async Task Register_ReturnsOkAndForwardsExpectedPayload()
    {
        const string JwtToken = "registration-jwt-token";
        var identityService = new CapturingIdentityService(
            ResultWith<string>.Success(JwtToken));

        var controller = new IdentityController(identityService);
        var webModel = new RegisterWebModel
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "Password123",
            FirstName = "Alice",
            LastName = "Johnson",
            Image = null
        };

        var response = await controller.Register(
            webModel,
            CancellationToken.None);

        var okObjectResult = Assert.IsType<OkObjectResult>(response.Result);
        var responseModel = Assert.IsType<JwtTokenServiceModel>(okObjectResult.Value);

        Assert.Equal(JwtToken, responseModel.Token);
        Assert.NotNull(identityService.RegisterInput);
        Assert.Equal(webModel.Username, identityService.RegisterInput!.Username);
        Assert.Equal(webModel.Email, identityService.RegisterInput.Email);
        Assert.Equal(webModel.Password, identityService.RegisterInput.Password);
        Assert.Equal(webModel.FirstName, identityService.RegisterInput.FirstName);
        Assert.Equal(webModel.LastName, identityService.RegisterInput.LastName);
        Assert.Null(identityService.RegisterInput.Image);
    }

    private sealed class CapturingIdentityService(
        ResultWith<string> registerResult) : IIdentityService
    {
        public RegisterServiceModel? RegisterInput { get; private set; }

        public Task<ResultWith<string>> Register(
            RegisterServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.RegisterInput = model;
            return Task.FromResult(registerResult);
        }

        public Task<ResultWith<string>> Login(
            LoginServiceModel model,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ResultWith<string>> ForgotPassword(
            ForgotPasswordServiceModel model,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ResultWith<string>> ResetPassword(
            ResetPasswordServiceModel model)
            => throw new NotSupportedException();
    }
}
