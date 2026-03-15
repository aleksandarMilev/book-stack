namespace BookStack.Infrastructure.Services.DateTimeProvider;

public interface IDateTimeProvider
{
    DateTime Now { get; }

    DateTime UtcNow { get; }
}
