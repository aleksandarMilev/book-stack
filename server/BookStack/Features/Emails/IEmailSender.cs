namespace BookStack.Features.Emails;

using Infrastructure.Services.ServiceLifetimes;

public interface IEmailSender : IScopedService
{
    Task SendWelcome(
        string email,
        string username,
        string baseUrl,
        CancellationToken cancellationToken = default);

    Task SendPasswordReset(
        string email,
        string resetUrl,
        CancellationToken cancellationToken = default);
}
