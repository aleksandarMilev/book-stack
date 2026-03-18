namespace BookStack.Features.Identity.Service.Models;

/// <summary>
/// API response model containing a user-facing status message.
/// </summary>
/// <param name="message">Status message returned by the service.</param>
public class MessageServiceModel(string message)
{
    /// <summary>
    /// Status message returned by the service.
    /// </summary>
    public string Message { get; init; } = message;
}
