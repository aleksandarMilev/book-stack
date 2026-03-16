namespace BookStack.Tests.TestInfrastructure;

using BookStack.Infrastructure.Services.DateTimeProvider;

internal sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    private DateTime _utcNow = DateTime
        .SpecifyKind(utcNow, DateTimeKind.Utc);

    public DateTime Now
        => this._utcNow.ToLocalTime();

    public DateTime UtcNow
    {
        get => this._utcNow;
        set => this._utcNow = DateTime
            .SpecifyKind(value, DateTimeKind.Utc);
    }
}
