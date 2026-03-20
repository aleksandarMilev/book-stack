namespace BookStack.Features.Emails;

using Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Templates;

public class EmailSender(
    IOptions<EmailSettings> settings,
    ILogger<EmailSender> logger) : IEmailSender
{
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
        using var client = new SmtpClient();

        try
        {
            var message = new MimeMessage();

            message.From.Add(MailboxAddress.Parse(settings.Value.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html")
            {
                Text = htmlBody
            };


            var secureOption = settings
                .Value
                .UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

            await client.ConnectAsync(
                settings.Value.Host,
                settings.Value.Port,
                secureOption,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(settings.Value.Username))
            {
                await client.AuthenticateAsync(
                    settings.Value.Username,
                    settings.Value.Password,
                    cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error sending email.");
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
