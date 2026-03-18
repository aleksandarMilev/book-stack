namespace BookStack.Features.Identity.Shared;

using Service.Models;
using UserProfile.Service.Models;
using Web.Models;

/// <summary>
/// Mapping helpers between identity web and service models.
/// </summary>
public static class IdentityMapping
{
    /// <summary>
    /// Maps login web payload to login service payload.
    /// </summary>
    /// <param name="webmodel">Incoming login web model.</param>
    /// <returns>Mapped service model for login processing.</returns>
    public static LoginServiceModel ToLoginServiceModel(
       this LoginWebModel webmodel)
       => new()
       {
           Credentials = webmodel.Credentials,
           Password = webmodel.Password,
           RememberMe = webmodel.RememberMe
       };

    /// <summary>
    /// Maps registration web payload to registration service payload.
    /// </summary>
    /// <param name="webmodel">Incoming registration web model.</param>
    /// <returns>Mapped service model for registration processing.</returns>
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

    /// <summary>
    /// Maps registration service payload to profile-creation payload.
    /// </summary>
    /// <param name="serviceModel">Registration service model.</param>
    /// <returns>Profile creation model derived from registration data.</returns>
    public static CreateProfileServiceModel ToCreateProfileServiceModel(
       this RegisterServiceModel serviceModel)
       => new()
       {
           FirstName = serviceModel.FirstName,
           LastName = serviceModel.LastName,
           Image = serviceModel.Image,
       };

    /// <summary>
    /// Maps forgot-password web payload to service payload.
    /// </summary>
    /// <param name="webModel">Incoming forgot-password web model.</param>
    /// <returns>Mapped forgot-password service model.</returns>
    public static ForgotPasswordServiceModel ToForgotPasswordServiceModel(
       this ForgotPasswordWebModel webModel)
       => new()
       {
           Email = webModel.Email
       };

    /// <summary>
    /// Maps reset-password web payload to service payload.
    /// </summary>
    /// <param name="webModel">Incoming reset-password web model.</param>
    /// <returns>Mapped reset-password service model.</returns>
    public static ResetPasswordServiceModel ToResetPasswordServiceModel(
        this ResetPasswordWebModel webModel)
        => new()
        {
            Email = webModel.Email,
            Token = webModel.Token,
            NewPassword = webModel.NewPassword
        };
}
