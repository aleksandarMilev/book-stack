namespace BookStack.Tests.TestInfrastructure.Factories;

using BookStack.Data;
using BookStack.Features.Identity.Data.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

internal static class TestUserManagerFactory
{
    public static UserManager<UserDbModel> Create(BookStackDbContext data)
    {
        var services = new ServiceCollection();

        services
            .AddLogging()
            .AddSingleton(
                DataProtectionProvider.Create("BookStack.Tests"))
            .AddIdentityCore<UserDbModel>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 3;

                options.User.RequireUniqueEmail = true;

                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<BookStackDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton(data);

        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<UserManager<UserDbModel>>();
    }
}