namespace BookStack.Features.Orders.Web;

public static class ApiRoutes
{
    public const string Mine = "mine/";

    public const string Sold = "sold/";

    public const string Status = "{id}/status/";

    public const string PaymentStatus = "{id}/payment-status/";

    public const string SettlementStatus = "{id}/settlement-status/";

    public const string SoldConfirm = $"{Sold}{{id}}/confirm/";

    public const string SoldShip = $"{Sold}{{id}}/ship/";

    public const string SoldDeliver = $"{Sold}{{id}}/deliver/";
}
