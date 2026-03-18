namespace BookStack.Features.Identity.Shared;

/// <summary>
/// Shared constants used by the identity feature.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Error messages returned by identity flows.
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        /// Generic message for failed login attempts.
        /// </summary>
        public const string InvalidLoginAttempt = "Invalid log in attempt!";

        /// <summary>
        /// Generic message for failed registration attempts.
        /// </summary>
        public const string InvalidRegisterAttempt = "Invalid register attempt!";

        /// <summary>
        /// Message returned after reaching failed-login lockout threshold.
        /// </summary>
        public const string AccountWasLocked = "Account locked due to multiple failed attempts.";

        /// <summary>
        /// Message returned when an already locked account tries to log in.
        /// </summary>
        public const string AccountIsLocked = "Account is locked. Try again later.";

        /// <summary>
        /// Generic message for invalid password reset attempts.
        /// </summary>
        public const string InvalidPasswordResetAttempt = "Invalid password reset attempt!";
    }

    /// <summary>
    /// Validation limits for identity web and service models.
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Minimum username length.
        /// </summary>
        public const int UsernameMinLength = 3;

        /// <summary>
        /// Maximum username length.
        /// </summary>
        public const int UsernameMaxLength = 30;

        /// <summary>
        /// Minimum password length.
        /// </summary>
        public const int PasswordMinLength = 6;

        /// <summary>
        /// Maximum password length.
        /// </summary>
        public const int PasswordMaxLength = 128;

        /// <summary>
        /// Minimum email length.
        /// </summary>
        public const int EmailMinLength = 5;

        /// <summary>
        /// Maximum email length.
        /// </summary>
        public const int EmailMaxLength = 254;

        /// <summary>
        /// Minimum length for login credentials field (username or email).
        /// </summary>
        public const int CredentialsMinLength = UsernameMinLength;

        /// <summary>
        /// Maximum length for login credentials field (username or email).
        /// </summary>
        public const int CredentialsMaxLength = EmailMaxLength;
    }

    /// <summary>
    /// Account lockout settings consumed by identity configuration.
    /// </summary>
    public static class Lockout
    {
        /// <summary>
        /// Lockout duration in minutes.
        /// </summary>
        public const int AccountLockoutTimeSpan = 15;

        /// <summary>
        /// Failed login attempts required before lockout.
        /// </summary>
        public const int MaxFailedLoginAttempts = 3;
    }

    /// <summary>
    /// JWT expiration settings in days.
    /// </summary>
    public static class TokenExpiration
    {
        /// <summary>
        /// Default token lifetime in days.
        /// </summary>
        public const int DefaultTokenExpirationTime = 7;

        /// <summary>
        /// Extended token lifetime in days when remember-me is enabled.
        /// </summary>
        public const int ExtendedTokenExpirationTime = 30;
    }
}
