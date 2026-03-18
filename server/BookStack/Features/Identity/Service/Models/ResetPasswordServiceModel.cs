namespace BookStack.Features.Identity.Service.Models;

/// <summary>
/// Service-layer payload for completing password reset.
/// </summary>
public class ResetPasswordServiceModel
{
    /// <summary>
    /// Email of the account whose password will be reset.
    /// </summary>
    public string Email { get; init; } = default!;

    /// <summary>
    /// Base64-url encoded reset token received from the reset link.
    /// </summary>
    public string Token { get; init; } = default!;

    /// <summary>
    /// New raw password.
    /// </summary>
    public string NewPassword { get; init; } = default!;
}
