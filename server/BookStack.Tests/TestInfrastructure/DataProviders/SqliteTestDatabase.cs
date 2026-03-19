namespace BookStack.Tests.TestInfrastructure.DataProviders;

using BookStack.Data;
using BookStack.Tests.Identity;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    public SqliteTestDatabase()
    {
        this.connection = new SqliteConnection("Data Source=:memory:");
        this.connection.Open();

        var user = new FakeCurrentUserService
        {
            UserId = "test-bootstrap-user",
            Username = "test-bootstrap-user",
            Admin = true,
        };

        var utc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FakeDateTimeProvider(utc);

        using var data = this.CreateDbContext(user, dateTimeProvider);
        data.Database.EnsureCreated();
    }

    public BookStackDbContext CreateDbContext(
        FakeCurrentUserService currentUserService,
        FakeDateTimeProvider dateTimeProvider)
    {
        var options = new DbContextOptionsBuilder<BookStackDbContext>()
            .UseSqlite(this.connection)
            .Options;

        return new BookStackDbContext(
            options,
            currentUserService,
            dateTimeProvider);
    }

    public IdentityTestFactory CreateIdentityFactory(
        FakeCurrentUserService currentUserService,
        FakeDateTimeProvider dateTimeProvider,
        out BookStackDbContext data)
    {
        data = this.CreateDbContext(
            currentUserService,
            dateTimeProvider);

        return new(
            data,
            currentUserService,
            dateTimeProvider);
    }

    public async ValueTask DisposeAsync()
    {
        await this.connection.CloseAsync();
        await this.connection.DisposeAsync();
    }
}
