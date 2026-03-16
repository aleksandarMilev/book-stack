namespace BookStack.Features.Payments.Service;

using Infrastructure.Services.Result;
using Microsoft.AspNetCore.Http;
using Models;

public interface IPaymentProvider
{
    string Name { get; }

    Task<ResultWith<PaymentProviderCheckoutResultServiceModel>> CreateCheckoutSession(
        PaymentProviderCheckoutRequestServiceModel model,
        CancellationToken cancellationToken = default);

    ResultWith<PaymentProviderWebhookEventServiceModel> ParseWebhook(
        string payload,
        IHeaderDictionary headers);
}
