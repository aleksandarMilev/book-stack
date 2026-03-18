namespace BookStack.Features.Identity.Service.Models;

/// <summary>
/// Service-layer payload used to create a new identity account and profile.
/// </summary>
public class RegisterServiceModel
{
    /// <summary>
    /// Desired username for the new account.
    /// </summary>
    public string Username { get; init; } = default!;

    /// <summary>
    /// Email address bound to the account.
    /// </summary>
    public string Email { get; init; } = default!;

    /// <summary>
    /// Raw password used by ASP.NET Identity during account creation.
    /// </summary>
    public string Password { get; init; } = default!;

    /// <summary>
    /// First name to persist in the linked profile.
    /// </summary>
    public string FirstName { get; init; } = default!;

    /// <summary>
    /// Last name to persist in the linked profile.
    /// </summary>
    public string LastName { get; init; } = default!;

    /// <summary>
    /// Optional profile image uploaded during registration.
    /// </summary>
    public IFormFile? Image { get; init; }
}
