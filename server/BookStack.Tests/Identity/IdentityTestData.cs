namespace BookStack.Tests.Identity;

using BookStack.Features.Identity.Service.Models;

internal static class IdentityTestData
{
    public static RegisterServiceModel CreateRegisterModel(
        string username = "alice",
        string email = "alice@example.com",
        string password = "123456",
        string firstName = "Alice",
        string lastName = "Tester")
        => new()
        {
            Username = username,
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName,
            Image = null,
        };

    public static LoginServiceModel CreateLoginModel(
        string credentials = "alice",
        string password = "123456",
        bool rememberMe = false)
        => new()
        {
            Credentials = credentials,
            Password = password,
            RememberMe = rememberMe,
        };

    public static ForgotPasswordServiceModel CreateForgotPasswordModel(
        string email = "alice@example.com")
        => new()
        {
            Email = email,
        };

    public static ResetPasswordServiceModel CreateResetPasswordModel(
        string email,
        string token,
        string newPassword = "654321")
        => new()
        {
            Email = email,
            Token = token,
            NewPassword = newPassword,
        };
}
