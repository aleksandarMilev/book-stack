namespace BookStack.Infrastructure.Extensions;

using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Data;
using Features.Identity.Data.Models;
using Features.Payments.Service;
using Features.Payments.Service.Providers;
using Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Outbox.Service;
using Serilog;
using Serilog.Events;
using Services.DateTimeProvider;
using Services.ServiceLifetimes;
using Settings;

using static Common.Constants;
using static Features.Identity.Shared.Constants;

public static class WebApplicationBuilderExtensions
{
    extension(IHostBuilder host)
    {
        public IHostBuilder AddLogging(IWebHostEnvironment env)
        {
            var logFilePath = Path.Combine(
                env.ContentRootPath,
                "logs",
                "app-.log");

            var directoryName = Path.GetDirectoryName(logFilePath)!;
            Directory.CreateDirectory(directoryName);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override(
                    "Microsoft",
                    LogEventLevel.Warning)
                .MinimumLevel.Override(
                    "Microsoft.AspNetCore",
                    LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 14)
                .CreateLogger();

            host.UseSerilog();

            return host;
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddCustomRequestTimeouts()
        {
            services.AddRequestTimeouts(static options =>
            {
                const int TimeoutDurationSeconds = 10;
                options.DefaultPolicy = new()
                {
                    Timeout = TimeSpan.FromSeconds(TimeoutDurationSeconds)
                };
            });

            return services;
        }

        public IServiceCollection AddCustomRateLimiter(
            IWebHostEnvironment env)
        {
            services.AddRateLimiter(options =>
            {
                options.OnRejected = static async (context, cancelationToken) =>
                {
                    context
                        .HttpContext
                        .Response
                        .StatusCode = StatusCodes.Status429TooManyRequests;

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context
                            .HttpContext
                            .Response
                            .Headers
                            .RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                    }

                    await context
                        .HttpContext
                        .Response
                        .WriteAsync("Too many requests.", cancelationToken);
                };

                options.GlobalLimiter = PartitionedRateLimiter
                    .Create<HttpContext, string>(httpContext =>
                    {
                        var ip = httpContext
                            .Connection
                            .RemoteIpAddress?
                            .ToString()
                            ?? "unknown";

                        return RateLimitPartition
                            .GetFixedWindowLimiter(ip, _ => new()
                            {
                                PermitLimit = env.IsDevelopment()
                                    ? 480
                                    : 240,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                                AutoReplenishment = true
                            });
                    });
            });

            return services;
        }

        public IServiceCollection AddCustomCorsPolicy(
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            if (env.IsEnvironment("Testing"))
            {
                return services;
            }

            const string ConfigSectionName = "Cors:AllowedOrigins";
            var allowedOrigins = configuration[ConfigSectionName];

            services.AddCors(options =>
            {
                options.AddPolicy(Cors.FrontendPolicyName, policy =>
                {
                    if (env.IsDevelopment())
                    {
                        policy
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(allowedOrigins))
                        {
                            var origins = allowedOrigins
                                .Split(
                                    ';',
                                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                            policy
                                .WithOrigins(origins)
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                "Cors:AllowedOrigins is not configured for the current environment.");
                        }
                    }
                });
            });

            return services;
        }

        public IServiceCollection AddAppSettings(
            IConfiguration configuration)
        {
            services.Configure<JwtSettings>(
                configuration.GetSection(nameof(JwtSettings)));

            services.Configure<EmailSettings>(
                configuration.GetSection(nameof(EmailSettings)));

            services.Configure<AppUrlsSettings>(
                configuration.GetSection(nameof(AppUrlsSettings)));

            services.Configure<PlatformFeeSettings>(
                configuration.GetSection(nameof(PlatformFeeSettings)));

            return services;
        }

        public IServiceCollection AddDatabase(
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            if (env.IsEnvironment("Testing"))
            {
                return services;
            }

            const string DefaultConnectionSection = "ConnectionStrings__DefaultConnection";
            const string DefaultConnection = "DefaultConnection";

            var connectionString = Environment
                .GetEnvironmentVariable(DefaultConnectionSection)
                ?? configuration.GetConnectionString(DefaultConnection);

            return services
                .AddDbContext<BookStackDbContext>(options =>
                {
                    options
                     .UseSqlServer(connectionString, static sqlOptions =>
                     {
                         sqlOptions.MigrationsAssembly(
                             typeof(BookStackDbContext).Assembly.FullName);

                         sqlOptions.EnableRetryOnFailure();
                     });
                });
        }

        public IServiceCollection AddIdentity(
            IWebHostEnvironment env)
        {
            services
                .AddIdentityCore<UserDbModel>(options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(Lockout.AccountLockoutTimeSpan);
                    options.Lockout.MaxFailedAccessAttempts = Lockout.MaxFailedLoginAttempts;

                    if (env.IsDevelopment() || env.IsEnvironment("Testing"))
                    {
                        const int RequiredDevLength = 6;

                        options.Password.RequireDigit = false;
                        options.Password.RequireLowercase = false;
                        options.Password.RequireUppercase = false;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequiredLength = RequiredDevLength;
                    }
                    else
                    {
                        const int RequiredProdLength = 8;

                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequiredLength = RequiredProdLength;
                    }
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<BookStackDbContext>()
                .AddDefaultTokenProviders();

            services
                .Configure<DataProtectionTokenProviderOptions>(static options =>
                {
                    const int LifeSpan = 2;
                    options.TokenLifespan = TimeSpan.FromHours(LifeSpan);
                });

            return services;
        }

        public IServiceCollection AddJwtAuthentication(
            IConfiguration _configuration,
            IWebHostEnvironment env)
        {
            services
                .AddAuthentication(static options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer();

            services
                .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
                {
                    var settings = jwtOptions.Value;

                    if (string.IsNullOrWhiteSpace(settings.Secret))
                    {
                        throw new InvalidOperationException("JwtSettings:Secret is missing or empty.");
                    }

                    if (string.IsNullOrWhiteSpace(settings.Issuer))
                    {
                        throw new InvalidOperationException("JwtSettings:Issuer is missing or empty.");
                    }

                    if (string.IsNullOrWhiteSpace(settings.Audience))
                    {
                        throw new InvalidOperationException("JwtSettings:Audience is missing or empty.");
                    }

                    const int ClockSkewMinutes = 2;

                    var keyBytes = Encoding.UTF8.GetBytes(settings.Secret);
                    var signingKey = new SymmetricSecurityKey(keyBytes)
                    {
                        KeyId = Jwt.SigningKeyId
                    };

                    options.MapInboundClaims = false;
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = !env.IsDevelopment();
                    options.IncludeErrorDetails = env.IsDevelopment();

                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        ValidateIssuer = true,
                        ValidIssuer = settings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = settings.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(ClockSkewMinutes),
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        TryAllIssuerSigningKeys = true
                    };

                    options.Events = new()
                    {
                        OnTokenValidated = async context =>
                        {
                            var userManager = context.HttpContext
                                .RequestServices
                                .GetRequiredService<UserManager<UserDbModel>>();

                            var principal = context.Principal;
                            if (principal is null)
                            {
                                context.Fail("Invalid token principal.");
                                return;
                            }

                            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                            var tokenSecurityStamp = principal.FindFirstValue("security_stamp");

                            if (string.IsNullOrWhiteSpace(userId))
                            {
                                context.Fail("Missing user id claim.");
                                return;
                            }

                            if (string.IsNullOrWhiteSpace(tokenSecurityStamp))
                            {
                                context.Fail("Missing security stamp claim.");
                                return;
                            }

                            var user = await userManager.FindByIdAsync(userId);
                            if (user is null)
                            {
                                context.Fail("User not found.");
                                return;
                            }

                            if (user.IsDeleted)
                            {
                                context.Fail("User is deleted.");
                                return;
                            }

                            var currentSecurityStamp = user.SecurityStamp ?? "";

                            if (!string.Equals(
                                    tokenSecurityStamp,
                                    currentSecurityStamp,
                                    StringComparison.Ordinal))
                            {
                                context.Fail("Token is no longer valid.");
                            }
                        }
                    };
                });

            return services;
        }

        public IServiceCollection AddSwagger()
        {
            const string Title = "BookStack API";
            const string Version = "v1";

            var apiInfo = new OpenApiInfo
            {
                Title = Title,
                Version = Version
            };

            services.AddSwaggerGen(
                options => options.SwaggerDoc(Version, apiInfo));

            return services;
        }

        public IServiceCollection AddServices()
        {
            Assembly
                .GetExecutingAssembly()
                .GetExportedTypes()
                .Where(static type => type.IsClass && !type.IsAbstract)
                .Select(static type => new
                {
                    Service = type.GetInterface($"I{type.Name}"),
                    Implementation = type

                })
                .Where(static type => type.Service is not null)
                .ToList()
                .ForEach(type =>
                {
                    if (typeof(ISingletonService).IsAssignableFrom(type.Service))
                    {
                        services.AddSingleton(type.Service, type.Implementation);
                    }

                    if (typeof(IScopedService).IsAssignableFrom(type.Service))
                    {
                        services.AddScoped(type.Service, type.Implementation);
                    }

                    if (typeof(ITransientService).IsAssignableFrom(type.Service))
                    {
                        services.AddTransient(type.Service, type.Implementation);
                    }
                });

            return services
                .AddSingleton<IDateTimeProvider, SystemDateTimeProvider>()
                .AddScoped<IPaymentProvider, MockPaymentProvider>()
                .AddHostedService<OutboxProcessor>();
        }

        public IServiceCollection AddApiControllers()
        {
            services
                .AddControllers(static options =>
                {
                    options
                        .Filters
                        .Add<ModelOrNotFoundActionFilter>();
                });

            return services;
        }

        public IServiceCollection AddCustomHealthChecks()
        {
            const string Name = "Database";

            services
                .AddHealthChecks()
                .AddDbContextCheck<BookStackDbContext>(Name);

            return services;
        }
    }
}
