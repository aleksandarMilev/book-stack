namespace BookStack.Features.UserProfile.Shared;

/// <summary>
/// Shared constants for the user-profile feature.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Validation limits for profile name fields.
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Minimum allowed length for first and last names.
        /// </summary>
        public const int NameMinLength = 2;

        /// <summary>
        /// Maximum allowed length for first and last names.
        /// </summary>
        public const int NameMaxLength = 100;
    }

    /// <summary>
    /// Image path constants used by profile image workflows.
    /// </summary>
    public static class Paths
    {
        /// <summary>
        /// Folder segment used for profile image storage.
        /// </summary>
        public const string ProfilesImagePathPrefix = "profiles";

        /// <summary>
        /// Relative path to the default profile image.
        /// </summary>
        public const string DefaultImagePath = $"/images/{ProfilesImagePathPrefix}/default.jpg";
    }
}
