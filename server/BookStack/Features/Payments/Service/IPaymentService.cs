namespace BookStack.Features.Payments.Service;

using Infrastructure.Services.Result;
using Infrastructure.Services.ServiceLifetimes;
using Microsoft.AspNetCore.Http;
using Models;
using Orders.Shared;

public interface IPaymentService : IScopedService
{
    Task<ResultWith<PaymentCheckoutSessionServiceModel>> CreateCheckoutSession(
        Guid orderId,
        CreatePaymentSessionServiceModel model,
        CancellationToken cancellationToken = default);

    Task<Result> ProcessWebhook(
        string provider,
        string payload,
        IHeaderDictionary headers,
        CancellationToken cancellationToken = default);

    Task<Result> ApplyManualPaymentStatus(
        Guid orderId,
        PaymentStatus paymentStatus,
        CancellationToken cancellationToken = default);

    Task ReleaseExpiredReservations(
        CancellationToken cancellationToken = default);

    Task<Result> ReleaseOrderReservation(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<Result> ExpireOrderReservation(
        Guid orderId,
        CancellationToken cancellationToken = default);
}
