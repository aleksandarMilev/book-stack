namespace BookStack.Features.Payments.Web;

public static class ApiRoutes
{
    public const string Checkout = "checkout/{orderId}/";

    public const string Webhook = "webhook/{provider}/";
}
