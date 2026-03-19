namespace BookStack.Infrastructure.Outbox.Service;

using System.Text.Json;
using BookStack.Data;
using Data.Models;
using Features.Emails;
using Features.Identity.Outbox;
using Microsoft.EntityFrameworkCore;
using Services.DateTimeProvider;

using static Common.Constants;

public sealed class OutboxProcessor(
    IServiceProvider serviceProvider,
    IDateTimeProvider dateTimeProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private readonly string workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Outbox processor started. WorkerId={WorkerId}",
            this.workerId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var processedCount = await this.ProcessBatch(cancellationToken);
                if (processedCount == 0)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(Processing.PollingIntervalSeconds),
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Outbox processor loop failed. WorkerId={WorkerId}",
                    this.workerId);

                await Task.Delay(
                    TimeSpan.FromSeconds(Processing.PollingIntervalSeconds),
                    cancellationToken);
            }
        }

        logger.LogInformation(
            "Outbox processor stopped. WorkerId={WorkerId}",
            this.workerId);
    }

    private async Task<int> ProcessBatch(
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var data = scope
            .ServiceProvider
            .GetRequiredService<BookStackDbContext>();

        var messages = await this.ClaimBatch(
            data,
            cancellationToken);

        if (messages.Count == 0)
        {
            return 0;
        }

        logger.LogInformation(
            "Claimed {Count} outbox message(s). WorkerId={WorkerId}",
            messages.Count,
            this.workerId);

        foreach (var message in messages)
        {
            try
            {
                await ProcessMessage(
                    scope.ServiceProvider,
                    message, cancellationToken);

                message.ProcessedOnUtc = dateTimeProvider.UtcNow;
                message.LastError = null;
                message.LockedBy = null;
                message.LockedUntilUtc = null;

                logger.LogInformation(
                    "Processed outbox message successfully. MessageId={MessageId}, Type={Type}, RetryCount={RetryCount}",
                    message.Id,
                    message.Type,
                    message.RetryCount);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.RetryCount += 1;
                message.LastError = Truncate(
                    exception.ToString(),
                    Processing.MaxLastErrorLength);

                message.NextAttemptOnUtc = this.CalculateNextAttemptUtc(message.RetryCount);
                message.LockedBy = null;
                message.LockedUntilUtc = null;

                if (message.RetryCount >= Processing.MaxRetryCount)
                {
                    logger.LogError(
                        exception,
                        "Outbox message failed and reached max retry count. MessageId={MessageId}, Type={Type}, RetryCount={RetryCount}",
                        message.Id,
                        message.Type,
                        message.RetryCount);
                }
                else
                {
                    logger.LogError(
                        exception,
                        "Failed processing outbox message. MessageId={MessageId}, Type={Type}, RetryCount={RetryCount}, NextAttemptOnUtc={NextAttemptOnUtc}",
                        message.Id,
                        message.Type,
                        message.RetryCount,
                        message.NextAttemptOnUtc);
                }
            }
        }

        await data.SaveChangesAsync(cancellationToken);

        return messages.Count;
    }

    private async Task<List<OutboxMessageDbModel>> ClaimBatch(
        BookStackDbContext data,
        CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;
        var lockUntilUtc = now.AddMinutes(Processing.LockDurationMinutes);

        var executionStrategy = data
            .Database
            .CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () => {
            await using var transaction = await data
               .Database
               .BeginTransactionAsync(cancellationToken);

            var messages = await data
                .OutboxMessages
                .Where(m =>
                    m.ProcessedOnUtc == null &&
                    m.RetryCount < Processing.MaxRetryCount &&
                    (m.NextAttemptOnUtc == null || m.NextAttemptOnUtc <= now) &&
                    (m.LockedUntilUtc == null || m.LockedUntilUtc <= now))
                .OrderBy(m => m.OccurredOnUtc)
                .Take(Processing.BatchSize)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return messages;
            }

            foreach (var message in messages)
            {
                message.LockedBy = this.workerId;
                message.LockedUntilUtc = lockUntilUtc;
            }

            await data.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return messages;
        });
    }

    private static Task ProcessMessage(
        IServiceProvider serviceProvider,
        OutboxMessageDbModel message,
        CancellationToken cancellationToken)
        => message.Type switch
        {
            MessageTypes.IdentityWelcomeEmailRequested =>
                ProcessWelcomeEmail(serviceProvider, message, cancellationToken),

            _ => throw new InvalidOperationException(
                $"Unsupported outbox message type '{message.Type}'.")
        };

    private static async Task ProcessWelcomeEmail(
        IServiceProvider serviceProvider,
        OutboxMessageDbModel message,
        CancellationToken cancellationToken)
    {
        var emailSender = serviceProvider
            .GetRequiredService<IEmailSender>();

        var payload = JsonSerializer
            .Deserialize<WelcomeEmailOutboxPayload>(message.PayloadJson)
            ?? throw new InvalidOperationException($"Outbox payload deserialized to null. MessageId={message.Id}");

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new InvalidOperationException(
                $"Welcome email payload is missing Email. MessageId={message.Id}");
        }

        if (string.IsNullOrWhiteSpace(payload.Username))
        {
            throw new InvalidOperationException(
                $"Welcome email payload is missing Username. MessageId={message.Id}");
        }

        if (string.IsNullOrWhiteSpace(payload.BaseUrl))
        {
            throw new InvalidOperationException(
                $"Welcome email payload is missing BaseUrl. MessageId={message.Id}");
        }

        await emailSender.SendWelcome(
            payload.Email,
            payload.Username,
            payload.BaseUrl,
            cancellationToken);
    }

    private DateTime CalculateNextAttemptUtc(int retryCount)
    {
        var delayMinutes = retryCount switch
        {
            <= 1 => 1,
            2 => 5,
            3 => 15,
            4 => 30,
            _ => 60
        };

        return dateTimeProvider
            .UtcNow
            .AddMinutes(delayMinutes);
    }

    private static string Truncate(
        string value, 
        int maxLength)
    {
        var shouldNotBeTruncated = 
            string.IsNullOrEmpty(value) ||
            value.Length <= maxLength;

        if (shouldNotBeTruncated)
        {
            return value;
        }

        return value[..maxLength];
    }
}
