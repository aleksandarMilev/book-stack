namespace BookStack.Tests.UserProfile.E2E;

using BookStack.Data;
using BookStack.Features.Emails;
using BookStack.Infrastructure.Services.DateTimeProvider;
using BookStack.Infrastructure.Services.ImageWriter;
using BookStack.Tests.TestInfrastructure.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

internal sealed class UserProfileApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection;

    public UserProfileApiWebApplicationFactory()
    {
        this.connection = new("Data Source=:memory:");
        this.connection.Open();

        var utc = new DateTime(2026, 03, 19, 10, 00, 00, DateTimeKind.Utc);
        this.DateTimeProvider = new(utc);

        this.EmailSender = new();
        this.ImageWriter = new();
    }

    public FakeDateTimeProvider DateTimeProvider { get; }

    public FakeEmailSender EmailSender { get; }

    public FakeImageWriter ImageWriter { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(static (_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "super_secret_test_key_12345678901234567890",
                ["JwtSettings:Issuer"] = "BookStack.Tests",
                ["JwtSettings:Audience"] = "BookStack.Tests.Client",
                ["AppUrlsSettings:ClientBaseUrl"] = "https://bookstack.test",
                ["PlatformFeeSettings:Percent"] = "10",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BookStackDbContext>>();
            services.RemoveAll<BookStackDbContext>();
            services.AddDbContext<BookStackDbContext>(
                options => options.UseSqlite(this.connection));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton(this.EmailSender);
            services.AddSingleton<IEmailSender>(this.EmailSender);

            services.RemoveAll<IImageWriter>();
            services.AddSingleton(this.ImageWriter);
            services.AddSingleton<IImageWriter>(this.ImageWriter);

            services.RemoveAll<IDateTimeProvider>();
            services.AddSingleton<IDateTimeProvider>(this.DateTimeProvider);

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var data = scope
                .ServiceProvider
                .GetRequiredService<BookStackDbContext>();

            data.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            this.connection.Close();
            this.connection.Dispose();
        }
    }
}

