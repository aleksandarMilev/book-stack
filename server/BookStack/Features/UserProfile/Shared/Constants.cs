namespace BookStack.Features.UserProfile.Shared;

public static class Constants
{
    public static class Validation
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 100;
    }

    public static class Paths
    {
        public const string ProfilesImagePathPrefix = "profiles";
        public const string DefaultImagePath = $"/images/{ProfilesImagePathPrefix}/default.jpg";
    }
}
