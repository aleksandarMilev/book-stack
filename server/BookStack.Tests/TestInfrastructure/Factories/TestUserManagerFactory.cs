namespace BookStack.Tests.TestInfrastructure.Factories;

using BookStack.Data;
using BookStack.Features.Identity.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

internal static class TestUserManagerFactory
{
    public static UserManager<UserDbModel> Create(BookStackDbContext data)
    {
        var store = new UserStore<UserDbModel>(data);

        var identityOptions = new IdentityOptions
        {
            Lockout =
            {
                AllowedForNewUsers = true,
                DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15),
                MaxFailedAccessAttempts = 3,
            },
            User =
            {
                RequireUniqueEmail = true,
            },
            Password =
            {
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireNonAlphanumeric = false,
                RequiredLength = 6,
            }
        };

        var options = Options.Create(identityOptions);
        var passwordHasher = new PasswordHasher<UserDbModel>();
        var userValidators = new List<IUserValidator<UserDbModel>>
        {
            new UserValidator<UserDbModel>(),
        };

        var passwordValidators = new List<IPasswordValidator<UserDbModel>>
        {
            new PasswordValidator<UserDbModel>(),
        };

        var normalizer = new UpperInvariantLookupNormalizer();
        var identityErrorDescriber = new IdentityErrorDescriber();
        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        return new UserManager<UserDbModel>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            normalizer,
            identityErrorDescriber,
            services,
            NullLogger<UserManager<UserDbModel>>.Instance);
    }
}
