namespace BookStack.Features.Payments.Service;

public class PaymentProviderRegistry(
    IEnumerable<IPaymentProvider> providers) : IPaymentProviderRegistry
{
    private readonly Dictionary<string, IPaymentProvider> _providers = providers
        .GroupBy(static p => p.Name, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            static group => group.Key,
            static group => group.First(),
            StringComparer.OrdinalIgnoreCase);

    public bool TryGetProvider(string providerName, out IPaymentProvider paymentProvider)
        => this._providers.TryGetValue(providerName, out paymentProvider!);
}
