namespace BookStack.Tests.TestInfrastructure.Fakes;

using BookStack.Infrastructure.Services.DateTimeProvider;

internal sealed class FakeDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    private DateTime utcNow = DateTime
        .SpecifyKind(utcNow, DateTimeKind.Utc);

    public DateTime Now
        => this.utcNow.ToLocalTime();

    public DateTime UtcNow
    {
        get=> this.utcNow;
        set => this.utcNow = DateTime
            .SpecifyKind(value, DateTimeKind.Utc);
    }
}
