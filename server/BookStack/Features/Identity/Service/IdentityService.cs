namespace BookStack.Features.Identity.Service;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BookStack.Data;
using Data.Models;
using Emails;
using Infrastructure.Outbox.Data.Models;
using Infrastructure.Services.DateTimeProvider;
using Infrastructure.Services.Result;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Models;
using Outbox;
using Shared;
using UserProfile.Service;

using static Common.Constants.Names;
using static Infrastructure.Outbox.Common.Constants;
using static Shared.Constants.ErrorMessages;
using static Shared.Constants.TokenExpiration;

/// <summary>
/// Implements registration, authentication, and password-reset workflows for identity users.
/// </summary>
/// <remarks>
/// Registration combines ASP.NET Identity user creation with profile creation and welcome-email outbox enqueueing.
/// Login uses lockout policies from <see cref="UserManager{TUser}"/>. Password-reset responses are intentionally generic
/// for unknown users to reduce user-enumeration risk.
/// </remarks>
public class IdentityService(
    BookStackDbContext data,
    UserManager<UserDbModel> userManager,
    IEmailSender emailSender,
    IProfileService profileService,
    ILogger<IdentityService> logger,
    IOptions<JwtSettings> jwtSettings,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppUrlsSettings> appUrlsSettings) : IIdentityService
{
    /// <summary>
    /// Creates a new identity user, creates the linked user profile, enqueues a welcome email, and returns a JWT.
    /// </summary>
    /// <param name="serviceModel">Registration payload.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Success result containing a JWT token, or failure result containing a validation/business error message.
    /// </returns>
    /// <remarks>
    /// The flow runs in a database transaction and rejects usernames that look like email addresses.
    /// </remarks>
    public async Task<ResultWith<string>> Register(
        RegisterServiceModel serviceModel,
        CancellationToken cancellationToken = default)
    {
        if (UsernameLooksLikeEmail(serviceModel.Username))
        {
            return ResultWith<string>.Failure(
                "Username cannot be in email format.");
        }

        var usersQuery = userManager
            .Users
            .IgnoreQueryFilters();

        var normalizedUsername = userManager
            .NormalizeName(serviceModel.Username);

        var normalizedEmail = userManager
            .NormalizeEmail(serviceModel.Email);

        var usernameTaken = await usersQuery
            .AnyAsync(
                u => u.NormalizedUserName == normalizedUsername,
                cancellationToken);

        if (usernameTaken)
        {
            return ResultWith<string>.Failure(
                $"Username '{serviceModel.Username}' is already taken.");
        }

        var emailTaken = await usersQuery
            .AnyAsync(
                u => u.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (emailTaken)
        {
            return ResultWith<string>.Failure(
                $"Email '{serviceModel.Email}' is already taken.");
        }

        var user = new UserDbModel
        {
            Email = serviceModel.Email,
            UserName = serviceModel.Username,
            LockoutEnabled = true
        };

        var executionStrategy = data.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await data
               .Database
               .BeginTransactionAsync(cancellationToken);

            try
            {
                var identityResult = await userManager.CreateAsync(
                    user,
                    serviceModel.Password);

                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    var identityResultError = identityResult
                        .Errors
                        .Select(static e => e.Description);

                    var errorMessage = string.Join("; ", identityResultError);

                    return ResultWith<string>
                        .Failure(errorMessage ?? InvalidRegisterAttempt);
                }

                await profileService.Create(
                    serviceModel.ToCreateProfileServiceModel(),
                    user.Id,
                    cancellationToken);

                var baseUrl = appUrlsSettings
                    .Value
                    .ClientBaseUrl?
                    .TrimEnd('/')
                    ?? throw new InvalidOperationException("AppUrlsSettings:ClientBaseUrl is not configured.");

                var payload = new WelcomeEmailOutboxPayload
                {
                    UserId = user.Id,
                    Email = serviceModel.Email,
                    Username = serviceModel.Username,
                    BaseUrl = baseUrl
                };

                var outboxMessageDbModel = new OutboxMessageDbModel
                {
                    Id = Guid.NewGuid(),
                    OccurredOnUtc = dateTimeProvider.UtcNow,
                    CreatedOnUtc = dateTimeProvider.UtcNow,
                    Type = MessageTypes.IdentityWelcomeEmailRequested,
                    PayloadJson = JsonSerializer.Serialize(payload),
                    NextAttemptOnUtc = dateTimeProvider.UtcNow
                };

                data.Add(outboxMessageDbModel);

                await data.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var jwt = GenerateJwtToken(user);

                logger.LogInformation(
                    "User successfully registered. UserId={UserId}",
                    user.Id);

                return ResultWith<string>.Success(jwt);
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Failed to complete registration. UserId={UserId}",
                    user.Id);

                await transaction.RollbackAsync(cancellationToken);

                return ResultWith<string>.Failure(InvalidRegisterAttempt);
            }
        });
    }

    /// <summary>
    /// Authenticates a user with username/email and password, enforcing lockout rules.
    /// </summary>
    /// <param name="serviceModel">Login payload.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Success result containing a JWT token, or failure result for invalid credentials or lockout scenarios.
    /// </returns>
    public async Task<ResultWith<string>> Login(
        LoginServiceModel serviceModel,
        CancellationToken cancellationToken = default)
    {
        var user = await this.FindUserForLogin(serviceModel.Credentials);

        if (user is null || user.IsDeleted)
        {
            return ResultWith<string>.Failure(InvalidLoginAttempt);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return ResultWith<string>.Failure(AccountIsLocked);
        }

        var passwordIsValid = await userManager.CheckPasswordAsync(
            user,
            serviceModel.Password);

        if (passwordIsValid)
        {
            await userManager.ResetAccessFailedCountAsync(user);

            var isAdmin = await userManager.IsInRoleAsync(
                user,
                AdminRoleName);

            var jwt = this.GenerateJwtToken(
                user,
                serviceModel.RememberMe,
                isAdmin);

            return ResultWith<string>.Success(jwt);
        }

        await userManager.AccessFailedAsync(user);

        if (await userManager.IsLockedOutAsync(user))
        {
            return ResultWith<string>.Failure(AccountWasLocked);
        }

        return ResultWith<string>.Failure(InvalidLoginAttempt);
    }

    /// <summary>
    /// Generates a password reset token and sends reset instructions when the target account exists.
    /// </summary>
    /// <param name="serviceModel">Forgot-password payload.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A generic success message, regardless of whether a matching account exists.</returns>
    /// <remarks>
    /// The method intentionally returns a generic message to avoid exposing account existence.
    /// </remarks>
    public async Task<ResultWith<string>> ForgotPassword(
        ForgotPasswordServiceModel serviceModel,
        CancellationToken cancellationToken = default)
    {
        const string GenericMessage =
            "If an account exists for that email, a password reset link has been sent.";

        var user = await userManager
            .FindByEmailAsync(serviceModel.Email);

        if (user is null || user.IsDeleted)
        {
            return ResultWith<string>.Success(GenericMessage);
        }

        try
        {
            var passwordResetToken = await userManager
                .GeneratePasswordResetTokenAsync(user);

            var passwordResetTokenAsByteArray = Encoding
                .UTF8
                .GetBytes(passwordResetToken);

            var encodedToken = WebEncoders
                .Base64UrlEncode(passwordResetTokenAsByteArray);

            var baseUrl = appUrlsSettings
                .Value
                .ClientBaseUrl?
                .TrimEnd('/')
                ?? throw new InvalidOperationException("AppUrlsSettings:ClientBaseUrl is not configured!");

            var resetPath = $"{baseUrl}/identity/reset-password";
            var resetUrl = QueryHelpers.AddQueryString(
                resetPath,
                new Dictionary<string, string?>
                {
                    ["email"] = user.Email,
                    ["token"] = encodedToken
                });

            await emailSender.SendPasswordReset(
                user.Email!,
                resetUrl,
                cancellationToken);

            return ResultWith<string>.Success(GenericMessage);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed generating/sending password reset email. UserId={UserId}",
                user.Id);

            return ResultWith<string>.Success(GenericMessage);
        }
    }

    /// <summary>
    /// Resets the password for a user when the supplied email and encoded reset token are valid.
    /// </summary>
    /// <param name="serviceModel">Reset-password payload.</param>
    /// <returns>
    /// Success result with a completion message, or failure result with an invalid-token/error description.
    /// </returns>
    /// <remarks>
    /// After a successful password reset, this method updates the security stamp to invalidate existing sessions.
    /// </remarks>
    public async Task<ResultWith<string>> ResetPassword(
        ResetPasswordServiceModel serviceModel)
    {
        var user = await userManager
            .FindByEmailAsync(serviceModel.Email);

        if (user is null || user.IsDeleted)
        {
            return ResultWith<string>.Failure(InvalidPasswordResetAttempt);
        }

        string token;

        try
        {
            var tokenBytes = WebEncoders.Base64UrlDecode(serviceModel.Token);
            token = Encoding.UTF8.GetString(tokenBytes);
        }
        catch
        {
            return ResultWith<string>.Failure(InvalidPasswordResetAttempt);
        }

        var result = await userManager.ResetPasswordAsync(
            user,
            token,
            serviceModel.NewPassword);

        if (!result.Succeeded)
        {
            var identityResultError = result
                .Errors
                .Select(static e => e.Description);

            var errorMessage = string.Join("; ", identityResultError);

            return ResultWith<string>
                .Failure(errorMessage ?? InvalidPasswordResetAttempt);
        }

        var updateSecurityStampResult = await userManager
            .UpdateSecurityStampAsync(user);

        if (!updateSecurityStampResult.Succeeded)
        {
            var errorMessage = string.Join(
                "; ",
                updateSecurityStampResult.Errors.Select(static e => e.Description));

            logger.LogError(
                "Password reset succeeded, but failed to update security stamp. UserId={UserId}, Errors={Errors}",
                user.Id,
                errorMessage);

            return ResultWith<string>.Failure(
                "Password was reset, but session invalidation failed. Please contact support.");
        }

        return ResultWith<string>.Success("Password successfully reset.");
    }

    /// <summary>
    /// Generates a signed JWT containing identity and optional admin claims.
    /// </summary>
    /// <param name="user">Authenticated user for whom the token is generated.</param>
    /// <param name="rememberMe">
    /// When <see langword="true"/>, uses extended token expiration; otherwise uses the default expiration window.
    /// </param>
    /// <param name="isAdmin">Indicates whether to include the admin role claim.</param>
    /// <returns>A serialized JWT token string.</returns>
    private string GenerateJwtToken(
        UserDbModel user,
        bool rememberMe = false,
        bool isAdmin = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();

        var secret = jwtSettings.Value.Secret;
        var key = Encoding.ASCII.GetBytes(secret);

        var claimList = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("security_stamp", user.SecurityStamp ?? string.Empty)
        };

        if (isAdmin)
        {
            claimList.Add(new(ClaimTypes.Role, AdminRoleName));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimList),
            Expires = rememberMe
                ? dateTimeProvider.UtcNow.AddDays(ExtendedTokenExpirationTime)
                : dateTimeProvider.UtcNow.AddDays(DefaultTokenExpirationTime),
            Issuer = jwtSettings.Value.Issuer,
            Audience = jwtSettings.Value.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Resolves a user by username or email, depending on credential format.
    /// </summary>
    /// <param name="credentials">User-supplied login identifier.</param>
    /// <returns>The matched user or <see langword="null"/> when no match exists.</returns>
    private async Task<UserDbModel?> FindUserForLogin(string credentials)
    {
        if (string.IsNullOrWhiteSpace(credentials))
        {
            return null;
        }

        credentials = credentials.Trim();
        if (credentials.Contains('@'))
        {
            return await userManager.FindByEmailAsync(credentials);
        }

        return await userManager.FindByNameAsync(credentials);
    }

    /// <summary>
    /// Determines whether the provided username appears to be an email address.
    /// </summary>
    /// <param name="username">Username candidate to evaluate.</param>
    /// <returns><see langword="true"/> when the username contains an '@' character; otherwise <see langword="false"/>.</returns>
    private static bool UsernameLooksLikeEmail(string username)
        => username.Trim().Contains('@');
}
