namespace BookStack.Infrastructure.Outbox.Common;

public static class Constants
{
    public static class Validation
    {
        public const int TypeMaxLength = 200;
    }

    public static class MessageTypes
    {
        public const string IdentityWelcomeEmailRequested = "Identity.WelcomeEmailRequested";
    }

    public static class Processing
    {
        public const int BatchSize = 20;

        public const int PollingIntervalSeconds = 10;

        public const int LockDurationMinutes = 2;

        public const int MaxRetryCount = 10;

        public const int MaxLastErrorLength = 4_000;
    }
}
