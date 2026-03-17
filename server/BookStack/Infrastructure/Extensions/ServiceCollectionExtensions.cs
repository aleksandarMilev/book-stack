namespace BookStack.Infrastructure.Extensions;

using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Data;
using Features.Identity.Data.Models;
using Features.Payments.Service;
using Features.Payments.Service.Providers;
using Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Services.DateTimeProvider;
using Services.ServiceLifetimes;
using Settings;

using static Common.Constants;
using static Features.Identity.Shared.Constants;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCustomHttpLogging(
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                services.AddHttpLogging(static options =>
                {
                    options.LoggingFields = HttpLoggingFields.All;
                });
            }
            else
            {
                services.AddHttpLogging(static options =>
                {
                    options.LoggingFields =
                        HttpLoggingFields.RequestMethod |
                        HttpLoggingFields.RequestPath |
                        HttpLoggingFields.RequestQuery |
                        HttpLoggingFields.ResponseStatusCode;
                });
            }

            return services;
        }

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

                    if (env.IsDevelopment())
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
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            var settings = configuration
                .GetSection(nameof(JwtSettings))
                .Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings section is missing!");

            var key = Encoding.ASCII.GetBytes(settings.Secret);

            services
                .AddAuthentication(static options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    if (env.IsDevelopment())
                    {
                        options.RequireHttpsMetadata = false;
                        options.IncludeErrorDetails = true;

                        options.TokenValidationParameters = new()
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateLifetime = true
                        };
                    }
                    else
                    {
                        const int ClockSkewMinutes = 2;

                        options.RequireHttpsMetadata = true;
                        options.TokenValidationParameters = new()
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = true,
                            ValidIssuer = settings.Issuer,
                            ValidateAudience = true,
                            ValidAudience = settings.Audience,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromMinutes(ClockSkewMinutes)
                        };
                    }
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

            services
                .AddSwaggerGen(options => options.SwaggerDoc(Version, apiInfo));

            return services;
        }

        public IServiceCollection AddServices()
        {
            var singletonInterfaceType = typeof(ISingletonService);
            var scopedInterfaceType = typeof(IScopedService);
            var transientInterfaceType = typeof(ITransientService);

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
                    if (singletonInterfaceType.IsAssignableFrom(type.Service))
                    {
                        services.AddSingleton(type.Service, type.Implementation);
                    }

                    if (scopedInterfaceType.IsAssignableFrom(type.Service))
                    {
                        services.AddScoped(type.Service, type.Implementation);
                    }

                    if (transientInterfaceType.IsAssignableFrom(type.Service))
                    {
                        services.AddTransient(type.Service, type.Implementation);
                    }
                });

            services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
            services.AddScoped<IPaymentProvider, MockPaymentProvider>();

            return services;
        }

        public IServiceCollection AddApiControllers()
        {
            services
                .AddControllers(static options =>
                {
                    options.Filters.Add<ModelOrNotFoundActionFilter>();
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
