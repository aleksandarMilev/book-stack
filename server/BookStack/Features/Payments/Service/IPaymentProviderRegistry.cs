namespace BookStack.Features.Payments.Service;

using Infrastructure.Services.ServiceLifetimes;

public interface IPaymentProviderRegistry : IScopedService
{
    bool TryGetProvider(
        string providerName,
        out IPaymentProvider paymentProvider);
}
