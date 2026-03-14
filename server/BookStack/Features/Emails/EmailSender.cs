namespace BookStack.Features.Emails;

using Templates;
using Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public class EmailSender(
    IOptions<EmailSettings> emailSettings,
    ILogger<EmailSender> logger) : IEmailSender
{
    private readonly EmailSettings settings = emailSettings.Value;
    private readonly ILogger<EmailSender> _logger = logger;

    public async Task SendWelcome(
        string email,
        string username,
        string baseUrl,
        CancellationToken cancellationToken = default)
        => await this.Send(
            email,
            "Welcome to BookStack 📚",
            WelcomeEmailTemplate.Build(
                username,
                baseUrl),
            cancellationToken);

    public async Task SendPasswordReset(
        string email,
        string resetUrl,
        CancellationToken cancellationToken = default)
        => await this.Send(
            email,
            "Reset your BookStack password",
            PasswordResetEmailTemplate.Build(resetUrl),
            cancellationToken);

    private async Task Send(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();

        message.From.Add(MailboxAddress.Parse(this.settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html")
        {
            Text = htmlBody
        };

        using var client = new SmtpClient();

        try
        {
            var secureOption = this.settings
                .UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

            await client.ConnectAsync(
                this.settings.Host,
                this.settings.Port,
                secureOption,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(this.settings.Username))
            {
                await client.AuthenticateAsync(
                    this.settings.Username,
                    this.settings.Password,
                    cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            this._logger.LogError(exception, "Error sending email.");
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
