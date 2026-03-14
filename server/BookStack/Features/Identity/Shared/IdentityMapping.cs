namespace BookStack.Features.Identity.Shared;

using Service.Models;
using UserProfile.Service.Models;
using Web.Models;

public static class IdentityMapping
{
    public static LoginServiceModel ToLoginServiceModel(
       this LoginWebModel webmodel)
       => new()
       {
           Credentials = webmodel.Credentials,
           Password = webmodel.Password,
           RememberMe = webmodel.RememberMe
       };

    public static RegisterServiceModel ToRegisterServiceModel(
        this RegisterWebModel webmodel)
        => new()
        {
            Username = webmodel.Username,
            Password = webmodel.Password,
            Email = webmodel.Email,
            FirstName = webmodel.FirstName,
            LastName = webmodel.LastName,
            Image = webmodel.Image,
        };

    public static CreateProfileServiceModel ToCreateProfileServiceModel(
       this RegisterServiceModel serviceModel)
       => new()
       {
           FirstName = serviceModel.FirstName,
           LastName = serviceModel.LastName,
           Image = serviceModel.Image,
       };

    public static ForgotPasswordServiceModel ToForgotPasswordServiceModel(
       this ForgotPasswordWebModel webModel)
       => new()
       {
           Email = webModel.Email
       };

    public static ResetPasswordServiceModel ToResetPasswordServiceModel(
        this ResetPasswordWebModel webModel)
        => new()
        {
            Email = webModel.Email,
            Token = webModel.Token,
            NewPassword = webModel.NewPassword
        };
}
