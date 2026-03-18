namespace BookStack.Infrastructure.Extensions;

using Data;
using Data.Seeders.Books;
using Data.Seeders.Listings;
using Features.Identity.Data.Models;
using Features.Identity.Service;
using Features.Identity.Service.Models;
using Features.SellerProfiles.Service;
using Features.SellerProfiles.Service.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata;
using static Common.Constants;

public static class AppBuilderExtensions
{
    public static IApplicationBuilder UseCustomForwardedHeaders(
        this IApplicationBuilder app)
    {
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto,
        };

        forwardedHeadersOptions.KnownProxies.Clear();

        app.UseForwardedHeaders(forwardedHeadersOptions);

        return app;
    }

    public static async Task<IApplicationBuilder> UseDevDb(
        this IApplicationBuilder app,
        CancellationToken cancellationToken)
    {
        await app.UseDevMigrations(cancellationToken);
        await app.UseDevAdminRole();
        await app.UseDevBuiltInUserBuyer(cancellationToken);
        await app.UseDevBuiltInUserSeller(cancellationToken);
        await app.UseDevCanonicalBookData(cancellationToken);
        await app.UseDevListingBookData(cancellationToken);

        return app;
    }

    public static IApplicationBuilder UseAppEndpoints(
        this IApplicationBuilder app)
    {
        app.UseEndpoints(static endpoints =>
        {
            endpoints
                .MapControllers();

            endpoints.MapHealthChecks("/health");
        });

        return app;
    }

    public static IApplicationBuilder UseSwaggerUI(
        this IApplicationBuilder app)
        => app
            .UseSwagger()
            .UseSwaggerUI(static options =>
            {
                const string Url = "/swagger/v1/swagger.json";
                const string Name = "BookStack API";

                options.SwaggerEndpoint(Url, Name);
                options.RoutePrefix = string.Empty;
            });

    public static IApplicationBuilder UseAllowedCors(
        this IApplicationBuilder app)
        => app.UseCors(Cors.FrontendPolicyName);

    private static async Task<IApplicationBuilder> UseDevMigrations(
        this IApplicationBuilder app,
        CancellationToken cancellationToken = default)
    {
        using var services = app
            .ApplicationServices
            .CreateScope();

        var data = services
            .ServiceProvider
            .GetRequiredService<BookStackDbContext>();

        await data
            .Database
            .MigrateAsync(cancellationToken);

        return app;
    }

    private static async Task<IApplicationBuilder> UseDevBuiltInUserBuyer(
        this IApplicationBuilder app,
        CancellationToken cancellationToken = default)
    {
        using var serviceScope = app
            .ApplicationServices
            .CreateScope();

        var services = serviceScope.ServiceProvider;

        var userManager = services
            .GetRequiredService<UserManager<UserDbModel>>();

        var config = services
            .GetRequiredService<IConfiguration>();

        var buyerEmail = config["BootstrapSeedUserBuyer:Email"]
            ?? "seed-buyer@localhost";

        var buyer = await userManager
            .FindByEmailAsync(buyerEmail);

        if (buyer is not null)
        {
            return app;
        }

        var identityService = services
            .GetRequiredService<IIdentityService>();

        var serviceModel = new RegisterServiceModel()
        {
            Username = config["BootstrapSeedUserBuyer:Username"] ?? "seed-buyer",
            Email = buyerEmail,
            Password = config["BootstrapSeedUserBuyer:Password"] ?? "123456",
            FirstName = config["BootstrapSeedUserBuyer:FirstName"] ?? "Seed",
            LastName = config["BootstrapSeedUserBuyer:LastName"] ?? "Buyer",
            Image = null,
        };

        await identityService.Register(
            serviceModel,
            cancellationToken);

        return app;
    }

    private static async Task<IApplicationBuilder> UseDevBuiltInUserSeller(
        this IApplicationBuilder app,
        CancellationToken cancellationToken = default)
    {
        using var serviceScope = app
            .ApplicationServices
            .CreateScope();

        var services = serviceScope.ServiceProvider;

        var userManager = services
            .GetRequiredService<UserManager<UserDbModel>>();

        var config = services
           .GetRequiredService<IConfiguration>();

        var sellerEmail = config["BootstrapSeedUserSeller:Email"]
            ?? "seed-seller@localhost";

        var seller = await userManager
           .FindByEmailAsync(sellerEmail);

        if (seller is not null)
        {
            return app;
        }

        var identityService = services
            .GetRequiredService<IIdentityService>();

        var registerServiceModel = new RegisterServiceModel()
        {
            Username = config["BootstrapSeedUserSeller:Username"] ?? "seed-seller",
            Email = sellerEmail,
            Password = config["BootstrapSeedUserSeller:Password"] ?? "123456",
            FirstName = config["BootstrapSeedUserSeller:FirstName"] ?? "Seed",
            LastName = config["BootstrapSeedUserSeller:LastName"] ?? "Seller",
            Image = null,
        };

        await identityService.Register(
            registerServiceModel,
            cancellationToken);

        var sellerService = services
           .GetRequiredService<ISellerProfileService>();

        var becomeSellerServiceModel = new UpsertSellerProfileServiceModel()
        {
            DisplayName = config["BootstrapSeedUserSeller:DisplayName"] ?? "DevSeller",
            PhoneNumber = config["BootstrapSeedUserSeller:PhoneNumber"] ?? "0898989898",
            SupportsCashOnDelivery = bool.Parse(config["BootstrapSeedUserSeller:SupportsCashOnDelivery"] ?? "true"),
            SupportsOnlinePayment = bool.Parse(config["BootstrapSeedUserSeller:SupportsOnlinePayment"] ?? "true"),
        };

        var userProfile = await userManager
            .FindByEmailAsync(config["BootstrapSeedUserSeller:Email"] ?? "seed-seller@localhost")
            ?? throw new("Seller's normal profile was not created successfully!");

        await sellerService.UpsertForUser(
            userProfile.Id,
            becomeSellerServiceModel,
            cancellationToken);

        return app;
    }

    private static async Task<IApplicationBuilder> UseDevCanonicalBookData(
        this IApplicationBuilder app,
        CancellationToken cancellationToken = default)
    {
        using var serviceScope = app
            .ApplicationServices
            .CreateScope();

        var seeder = serviceScope
            .ServiceProvider
            .GetRequiredService<IBookSeeder>();

        await seeder.Seed(cancellationToken);

        return app;
    }

    private static async Task<IApplicationBuilder> UseDevListingBookData(
        this IApplicationBuilder app,
        CancellationToken cancellationToken = default)
    {
        using var serviceScope = app
            .ApplicationServices
            .CreateScope();

        var seeder = serviceScope
            .ServiceProvider
            .GetRequiredService<IListingsSeeder>();

        await seeder.Seed(cancellationToken);

        return app;
    }

    private static async Task<IApplicationBuilder> UseDevAdminRole(
        this IApplicationBuilder app)
    {
        using var serviceScope = app
            .ApplicationServices
            .CreateScope();

        var services = serviceScope.ServiceProvider;

        var userManager = services
            .GetRequiredService<UserManager<UserDbModel>>();

        var roleManager = services
            .GetRequiredService<RoleManager<IdentityRole>>();

        if (await roleManager.RoleExistsAsync(Names.AdminRoleName))
        {
            return app;
        }

        var role = new IdentityRole
        {
            Name = Names.AdminRoleName
        };

        await roleManager.CreateAsync(role);

        var config = services
            .GetRequiredService<IConfiguration>();

        var adminEmail = config["BootstrapAdmin:DevEmail"] ?? "admin@localhost";
        var adminPassword = config["BootstrapAdmin:DevPassword"] ?? "123456";

        var user = new UserDbModel
        {
            Email = adminEmail,
            UserName = Names.AdminRoleName
        };

        await userManager.CreateAsync(user, adminPassword);
        await userManager.AddToRoleAsync(user, role.Name);

        return app;
    }

    // We need this method to create administrator if we drop the production db for some reason.
    // Don't delete it!
    private static async Task<IApplicationBuilder> UseProductionAdminRole(
        this IApplicationBuilder app)
    {
        using var scope = app
            .ApplicationServices
            .CreateScope();

        var services = scope.ServiceProvider;

        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("BootstrapAdmin");

        var config = services.GetRequiredService<IConfiguration>();
        var enabled = string.Equals(
            config["BootstrapAdmin:Enabled"],
            "true",
            StringComparison.OrdinalIgnoreCase);

        logger.LogInformation("BootstrapAdmin Enabled = {Enabled}", enabled);

        if (!enabled)
        {
            return app;
        }

        var email = config["BootstrapAdmin:Email"];
        var password = config["BootstrapAdmin:Password"];
        var roleName = config["BootstrapAdmin:Role"] ?? Names.AdminRoleName;

        var emailOrPasswordIsNotProvided =
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password);

        if (emailOrPasswordIsNotProvided)
        {
            throw new InvalidOperationException(
                "BootstrapAdmin enabled but Email/Password not set.");
        }

        var userManager = services
            .GetRequiredService<UserManager<UserDbModel>>();

        var roleManager = services
            .GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = new IdentityRole(roleName);
            var createRoleResult = await roleManager.CreateAsync(role);

            if (!createRoleResult.Succeeded)
            {
                var roleResultErrorMessage = createRoleResult
                    .Errors
                    .Select(static e => e.Description);

                throw new InvalidOperationException(
                    "Failed to create role: " + string.Join("; ", roleResultErrorMessage));
            }

            logger.LogInformation("Created role {Role}", roleName);
        }
        else
        {
            logger.LogInformation("Role {Role} already exists", roleName);
        }

        var user = await userManager.FindByEmailAsync(email!);
        if (user is null)
        {
            user = new()
            {
                Email = email,
                UserName = email
            };

            var createUserResult = await userManager
                .CreateAsync(user, password!);

            if (!createUserResult.Succeeded)
            {
                var errorMessage = createUserResult
                    .Errors
                    .Select(static e => e.Description);

                throw new InvalidOperationException(
                    "Failed to create admin user: " + string.Join("; ", errorMessage));
            }

            logger.LogInformation("Created user {Email}", email);
        }
        else
        {
            logger.LogInformation("User {Email} already exists", email);
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var addToRoleResult = await userManager
                .AddToRoleAsync(user, roleName);

            if (!addToRoleResult.Succeeded)
            {
                var errorMessage = addToRoleResult
                    .Errors
                    .Select(static e => e.Description);

                throw new InvalidOperationException(
                    "Failed to add role: " + string.Join("; ", errorMessage));
            }

            logger.LogInformation(
                "Added user {Email} to role {Role}",
                email,
                roleName);
        }
        else
        {
            logger.LogInformation(
                "User {Email} already in role {Role}",
                email,
                roleName);
        }

        return app;
    }
}
