namespace BookStack.Features.Payments.Shared;

public static class Constants
{
    public static class Providers
    {
        public const string Mock = "mock";
        public const string ManualAdmin = "manual-admin";
    }

    public static class Validation
    {
        public const int ProviderMaxLength = 64;
        public const int ProviderPaymentIdMaxLength = 200;
        public const int ProviderEventIdMaxLength = 200;
        public const int CurrencyLength = 3;
        public const int FailureReasonMaxLength = 1_000;
        public const int ProcessingResultMaxLength = 200;
        public const int PayloadMaxLength = 4_000;
    }
}