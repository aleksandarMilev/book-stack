namespace BookStack.Infrastructure.Outbox.Data.Models;

public class OutboxMessageDbModel
{
    public Guid Id { get; set; }

    public DateTime OccurredOnUtc { get; set; }

    public string Type { get; set; } = default!;

    public string PayloadJson { get; set; } = default!;

    public DateTime? ProcessedOnUtc { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }

    public DateTime? NextAttemptOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public string? LockedBy { get; set; }
}

