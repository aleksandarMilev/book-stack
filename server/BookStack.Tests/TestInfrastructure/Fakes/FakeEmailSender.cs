namespace BookStack.Tests.TestInfrastructure.Fakes;

using BookStack.Features.Emails;

internal sealed class FakeEmailSender : IEmailSender
{
    public List<WelcomeEmailRecord> WelcomeEmails { get; } = [];

    public List<PasswordResetEmailRecord> PasswordResetEmails { get; } = [];

    public bool ThrowOnSendWelcome { get; set; }

    public bool ThrowOnSendPasswordReset { get; set; }

    public Task SendWelcome(
        string email,
        string username,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        if (this.ThrowOnSendWelcome)
        {
            throw new InvalidOperationException("Fake welcome email send failure.");
        }

        var welcomeEmailRecord = new WelcomeEmailRecord(
            email,
            username,
            baseUrl);

        this.WelcomeEmails.Add(welcomeEmailRecord);

        return Task.CompletedTask;
    }

    public Task SendPasswordReset(
        string email,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        if (this.ThrowOnSendPasswordReset)
        {
            throw new InvalidOperationException("Fake password reset email send failure.");
        }

        var passwordResetEmailRecord = new PasswordResetEmailRecord(
            email,
            resetUrl);

        this.PasswordResetEmails.Add(passwordResetEmailRecord);

        return Task.CompletedTask;
    }

    public sealed record WelcomeEmailRecord(
        string Email,
        string Username,
        string BaseUrl);

    public sealed record PasswordResetEmailRecord(
        string Email,
        string ResetUrl);
}
