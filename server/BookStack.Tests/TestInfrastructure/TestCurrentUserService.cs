namespace BookStack.Tests.TestInfrastructure;

using BookStack.Infrastructure.Services.CurrentUser;

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; }

    public string? Username { get; set; }

    public bool Admin { get; set; }

    public string? GetUsername()
        => this.Username;

    public string? GetId()
        => this.UserId;

    public bool IsAdmin()
        => this.Admin;
}
