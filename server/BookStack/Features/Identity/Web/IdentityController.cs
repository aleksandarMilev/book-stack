namespace BookStack.Features.Identity.Web;

using Common;
using Identity.Shared;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;

using static ApiRoutes;

public class IdentityController(IIdentityService service) : ApiController
{
    [HttpPost(RegisterRoute)]
    public async Task<ActionResult<JwtTokenServiceModel>> Register(
        RegisterWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToRegisterServiceModel();
        var result = await service.Register(
            serviceModel,
            cancellationToken);

        return this.OkOrBadRequest(
            result,
            static token => new JwtTokenServiceModel(token));
    }

    [HttpPost(LoginRoute)]
    public async Task<ActionResult<JwtTokenServiceModel>> Login(
        LoginWebModel webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToLoginServiceModel();
        var result = await service.Login(
            serviceModel,
            cancellationToken);

        return this.OkOrBadRequest(
            result,
            static token => new JwtTokenServiceModel(token));
    }

    [HttpPost(ForgotPasswordRoute)]
    public async Task<ActionResult<MessageServiceModel>> ForgotPassword(
       ForgotPasswordWebModel webModel,
       CancellationToken cancellationToken = default)
    {
        var serviceModel = webModel.ToForgotPasswordServiceModel();
        var result = await service.ForgotPassword(
            serviceModel,
            cancellationToken);

        return this.OkOrBadRequest(
            result,
            static message => new MessageServiceModel(message));
    }

    [HttpPost(ResetPasswordRoute)]
    public async Task<ActionResult<MessageServiceModel>> ResetPassword(
        ResetPasswordWebModel webModel)
    {
        var serviceModel = webModel.ToResetPasswordServiceModel();
        var result = await service.ResetPassword(
            serviceModel);

        return this.OkOrBadRequest(
            result,
            static message => new MessageServiceModel(message));
    }
}
