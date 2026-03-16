namespace BookStack.Tests.TestInfrastructure;

using BookStack.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

internal sealed class TestDatabaseScope : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    public TestDatabaseScope()
    {
        this.connection = new SqliteConnection("Data Source=:memory:");
        this.connection.Open();

        var user = new TestCurrentUserService
        {
            UserId = "test-bootstrap-user",
            Username = "test-bootstrap-user",
            Admin = true,
        };

        var utc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var clock = new TestDateTimeProvider(utc);

        using var data = this.CreateDbContext(user, clock);
        data.Database.EnsureCreated();
    }

    public BookStackDbContext CreateDbContext(
        TestCurrentUserService currentUserService,
        TestDateTimeProvider dateTimeProvider)
    {
        var options = new DbContextOptionsBuilder<BookStackDbContext>()
            .UseSqlite(this.connection)
            .Options;

        return new(
            options,
            currentUserService,
            dateTimeProvider);
    }

    public async ValueTask DisposeAsync()
    {
        await this.connection.CloseAsync();
        await this.connection.DisposeAsync();
    }
}
