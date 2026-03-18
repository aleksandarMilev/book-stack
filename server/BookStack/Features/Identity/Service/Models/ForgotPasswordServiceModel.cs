namespace BookStack.Features.Identity.Service.Models;

/// <summary>
/// Service-layer payload for initiating password reset.
/// </summary>
public class ForgotPasswordServiceModel
{
    /// <summary>
    /// Account email that should receive a reset link.
    /// </summary>
    public string Email { get; init; } = default!;
}
