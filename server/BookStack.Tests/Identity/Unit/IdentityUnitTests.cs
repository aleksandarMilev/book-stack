namespace BookStack.Tests.Identity.Unit;

using BookStack.Features.Identity.Service;
using BookStack.Features.Identity.Service.Models;
using BookStack.Features.Identity.Web;
using BookStack.Features.Identity.Web.Models;
using BookStack.Infrastructure.Services.Result;
using Microsoft.AspNetCore.Mvc;

public class IdentityUnitTests
{
    [Fact]
    public async Task Register_ShouldReturnOk_WithJwtToken_WhenServiceSucceeds()
    {
        var fakeService = new FakeIdentityService
        {
            RegisterResult = ResultWith<string>.Success("jwt-token")
        };

        var controller = new IdentityController(fakeService);
        var model = new RegisterWebModel
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "123456",
            FirstName = "Alice",
            LastName = "Tester",
        };

        var result = await controller.Register(
            model,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<JwtTokenServiceModel>(okResult.Value);

        Assert.Equal("jwt-token", response.Token);
        Assert.NotNull(fakeService.LastRegisterModel);
        Assert.Equal(model.Username, fakeService.LastRegisterModel!.Username);
        Assert.Equal(model.Email, fakeService.LastRegisterModel.Email);
        Assert.Equal(model.FirstName, fakeService.LastRegisterModel.FirstName);
        Assert.Equal(model.LastName, fakeService.LastRegisterModel.LastName);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenServiceFails()
    {
        var fakeService = new FakeIdentityService
        {
            LoginResult = ResultWith<string>.Failure("Invalid login attempt!")
        };

        var controller = new IdentityController(fakeService);
        var model = new LoginWebModel
        {
            Credentials = "alice",
            Password = "wrong-pass"
        };

        var result = await controller.Login(
            model,
            CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(
            "Invalid login attempt!",
            GetErrorMessage(badRequestResult.Value));

        Assert.NotNull(fakeService.LastLoginModel);
        Assert.Equal(
            "alice",
            fakeService.LastLoginModel!.Credentials);

        Assert.Equal(
            "wrong-pass",
            fakeService.LastLoginModel.Password);
    }

    [Fact]
    public async Task ForgotPassword_ShouldMapInputAndReturnOk_WhenServiceSucceeds()
    {
        var fakeService = new FakeIdentityService
        {
            ForgotPasswordResult = ResultWith<string>.Success("generic-message")
        };

        var controller = new IdentityController(fakeService);
        var model = new ForgotPasswordWebModel
        {
            Email = "alice@example.com"
        };

        var result = await controller.ForgotPassword(
            model,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MessageServiceModel>(okResult.Value);

        Assert.Equal("generic-message", response.Message);
        Assert.NotNull(fakeService.LastForgotPasswordModel);
        Assert.Equal(
            "alice@example.com",
            fakeService.LastForgotPasswordModel!.Email);
    }

    [Fact]
    public async Task ResetPassword_ShouldMapInputAndReturnOk_WhenServiceSucceeds()
    {
        var fakeService = new FakeIdentityService
        {
            ResetPasswordResult = ResultWith<string>.Success("Password successfully reset.")
        };

        var controller = new IdentityController(fakeService);
        var model =  new ResetPasswordWebModel
        {
            Email = "alice@example.com",
            Token = "encoded-token",
            NewPassword = "654321"
        };

        var result = await controller.ResetPassword(model);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MessageServiceModel>(okResult.Value);

        Assert.Equal("Password successfully reset.", response.Message);
        Assert.NotNull(fakeService.LastResetPasswordModel);
        Assert.Equal("alice@example.com", fakeService.LastResetPasswordModel!.Email);
        Assert.Equal("encoded-token", fakeService.LastResetPasswordModel.Token);
        Assert.Equal("654321", fakeService.LastResetPasswordModel.NewPassword);
    }

    private static string? GetErrorMessage(object? errorObject)
        => errorObject?
            .GetType()
            .GetProperty("errorMessage")?
            .GetValue(errorObject) as string;

    private sealed class FakeIdentityService : IIdentityService
    {
        public ResultWith<string> RegisterResult { get; init; }
            = ResultWith<string>.Failure("register-failure");

        public ResultWith<string> LoginResult { get; init; }
            = ResultWith<string>.Failure("login-failure");

        public ResultWith<string> ForgotPasswordResult { get; init; }
            = ResultWith<string>.Failure("forgot-failure");

        public ResultWith<string> ResetPasswordResult { get; init; }
            = ResultWith<string>.Failure("reset-failure");

        public RegisterServiceModel? LastRegisterModel { get; private set; }

        public LoginServiceModel? LastLoginModel { get; private set; }

        public ForgotPasswordServiceModel? LastForgotPasswordModel { get; private set; }

        public ResetPasswordServiceModel? LastResetPasswordModel { get; private set; }

        public Task<ResultWith<string>> Register(
            RegisterServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.LastRegisterModel = model;
            return Task.FromResult(this.RegisterResult);
        }

        public Task<ResultWith<string>> Login(
            LoginServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.LastLoginModel = model;
            return Task.FromResult(this.LoginResult);
        }

        public Task<ResultWith<string>> ForgotPassword(
            ForgotPasswordServiceModel model,
            CancellationToken cancellationToken = default)
        {
            this.LastForgotPasswordModel = model;
            return Task.FromResult(this.ForgotPasswordResult);
        }

        public Task<ResultWith<string>> ResetPassword(
            ResetPasswordServiceModel model)
        {
            this.LastResetPasswordModel = model;
            return Task.FromResult(this.ResetPasswordResult);
        }
    }
}
