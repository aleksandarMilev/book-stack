namespace BookStack.Features.SellerProfiles.Shared;

/// <summary>
/// Shared constants used by the seller-profiles feature.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Validation limits for seller-profile web and service models.
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Minimum display-name length.
        /// </summary>
        public const int DisplayNameMinLength = 2;

        /// <summary>
        /// Maximum display-name length.
        /// </summary>
        public const int DisplayNameMaxLength = 150;

        /// <summary>
        /// Minimum phone-number length.
        /// </summary>
        public const int PhoneMinLength = 5;

        /// <summary>
        /// Maximum phone-number length.
        /// </summary>
        public const int PhoneMaxLength = 30;
    }
}
