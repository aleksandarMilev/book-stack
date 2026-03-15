namespace BookStack.Infrastructure.Services.DateTimeProvider;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now
        => DateTime.Now;

    public DateTime UtcNow
        => DateTime.UtcNow;
}
