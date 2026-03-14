namespace BookStack.Features.Identity.Service;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Data.Models;
using Emails;
using Infrastructure.Services.Result;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Models;
using Shared;
using UserProfile.Service;

using static Common.Constants.Names;
using static Shared.Constants.ErrorMessages;
using static Shared.Constants.TokenExpiration;

public class IdentityService(
    UserManager<UserDbModel> userManager,
    IEmailSender emailSender,
    IProfileService profileService,
    ILogger<IdentityService> logger,
    IOptions<JwtSettings> jwtSettings,
    IOptions<AppUrlsSettings> appUrlsSettings) : IIdentityService
{
    private readonly IProfileService _profileService = profileService;
    private readonly UserManager<UserDbModel> _userManager = userManager;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ILogger<IdentityService> _logger = logger;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly AppUrlsSettings _appUrlsSettings = appUrlsSettings.Value;

    public async Task<ResultWith<string>> Register(
        RegisterServiceModel serviceModel,
        CancellationToken cancellationToken = default)
    {
        var usersQuery = this._userManager.Users;
        usersQuery = usersQuery.IgnoreQueryFilters();

        var normalizedUsername = this
            ._userManager
            .NormalizeName(serviceModel.Username);

        var normalizedEmail = this
            ._userManager
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

        var identityResult = await this._userManager.CreateAsync(
            user,
            serviceModel.Password);

        if (identityResult.Succeeded)
        {
            try
            {
                var jwt = this.GenerateJwtToken(
                    this._jwtSettings.Secret,
                    user.Id,
                    serviceModel.Username,
                    serviceModel.Email);

                this._logger.LogInformation(
                    "User successfully registered. UserId={UserId}",
                    user.Id);

                await this._profileService.Create(
                    serviceModel.ToCreateProfileServiceModel(),
                    user.Id,
                    cancellationToken);

                var baseUrl = this._appUrlsSettings
                    .ClientBaseUrl?
                    .TrimEnd('/')
                    ?? throw new InvalidOperationException("AppUrlsSettings:ClientBaseUrl is not configured.");

                await this._emailSender.SendWelcome(
                    serviceModel.Email,
                    serviceModel.Username,
                    baseUrl,
                    cancellationToken);

                return ResultWith<string>.Success(jwt);
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception,
                    "Failed to complete registration. UserId={UserId}",
                    user.Id);

                await this._userManager.DeleteAsync(user);

                return ResultWith<string>.Failure(InvalidRegisterAttempt);
            }
        }

        var identityResultError = identityResult
            .Errors
            .Select(static e => e.Description);

        var errorMessage = string.Join("; ", identityResultError);

        return ResultWith<string>
            .Failure(errorMessage ?? InvalidRegisterAttempt);
    }

    public async Task<ResultWith<string>> Login(
        LoginServiceModel serviceModel,
        CancellationToken cancellationToken = default)
    {
        var user = await this._userManager
            .FindByNameAsync(serviceModel.Credentials);

        if (user is null)
        {
            user = await this._userManager.FindByEmailAsync(
                serviceModel.Credentials);

            if (user is null)
            {
                return ResultWith<string>.Failure(InvalidLoginAttempt);
            }
        }

        if (user.IsDeleted)
        {
            return ResultWith<string>.Failure(InvalidLoginAttempt);
        }

        if (await this._userManager.IsLockedOutAsync(user))
        {
            return ResultWith<string>.Failure(AccountIsLocked);
        }

        var passwordIsValid = await this._userManager.CheckPasswordAsync(
            user,
            serviceModel.Password);

        if (passwordIsValid)
        {
            await this._userManager.ResetAccessFailedCountAsync(user);

            var isAdmin = await this._userManager.IsInRoleAsync(
                user,
                AdminRoleName);

            var jwt = this.GenerateJwtToken(
                this._jwtSettings.Secret,
                user.Id,
                user.UserName!,
                user.Email!,
                serviceModel.RememberMe,
                isAdmin);

            return ResultWith<string>.Success(jwt);
        }

        await this._userManager.AccessFailedAsync(user);

        if (await this._userManager.IsLockedOutAsync(user))
        {
            return ResultWith<string>.Failure(AccountWasLocked);
        }

        return ResultWith<string>.Failure(InvalidLoginAttempt);
    }

    public async Task<ResultWith<string>> ForgotPassword(
        ForgotPasswordServiceModel serviceModel,
        CancellationToken cancellationToken = default)
    {
        const string GenericMessage =
            "If an account exists for that email, a password reset link has been sent.";

        var user = await this._userManager
            .FindByEmailAsync(serviceModel.Email);

        if (user is null || user.IsDeleted)
        {
            return ResultWith<string>.Success(GenericMessage);
        }

        try
        {
            var passwordResetToken = await this._userManager
                .GeneratePasswordResetTokenAsync(user);

            var passwordResetTokenAsByteArray = Encoding
                .UTF8
                .GetBytes(passwordResetToken);

            var encodedToken = WebEncoders
                .Base64UrlEncode(passwordResetTokenAsByteArray);

            var baseUrl = this._appUrlsSettings
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

            await this._emailSender.SendPasswordReset(
                user.Email!,
                resetUrl,
                cancellationToken);

            return ResultWith<string>.Success(GenericMessage);
        }
        catch (Exception exception)
        {
            this._logger.LogError(
                exception,
                "Failed generating/sending password reset email. UserId={UserId}",
                user.Id);

            return ResultWith<string>.Success(GenericMessage);
        }
    }

    public async Task<ResultWith<string>> ResetPassword(
        ResetPasswordServiceModel serviceModel)
    {
        var user = await this._userManager
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

        var result = await this._userManager.ResetPasswordAsync(
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

        return ResultWith<string>.Success("Password successfully reset.");
    }

    private string GenerateJwtToken(
        string appSettingsSecret,
        string userId,
        string username,
        string email,
        bool rememberMe = false,
        bool isAdmin = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();

        var key = Encoding.ASCII.GetBytes(appSettingsSecret);
        var claimList = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Email, email)
        };

        if (isAdmin)
        {
            claimList.Add(new(ClaimTypes.Role, AdminRoleName));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimList),
            Expires = rememberMe
                ? DateTime.UtcNow.AddDays(ExtendedTokenExpirationTime)
                : DateTime.UtcNow.AddDays(DefaultTokenExpirationTime),
            Issuer = jwtSettings.Value.Issuer,
            Audience = jwtSettings.Value.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
