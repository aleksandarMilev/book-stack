namespace BookStack.Features.Identity.Outbox;

/// <summary>
/// Payload persisted in outbox messages for deferred welcome-email sending.
/// </summary>
public sealed class WelcomeEmailOutboxPayload
{
    /// <summary>
    /// Identifier of the newly registered user.
    /// </summary>
    public string UserId { get; init; } = default!;

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string Email { get; init; } = default!;

    /// <summary>
    /// Username used in the welcome email content.
    /// </summary>
    public string Username { get; init; } = default!;

    /// <summary>
    /// Client application base URL used to build links in email templates.
    /// </summary>
    public string BaseUrl { get; init; } = default!;
}
