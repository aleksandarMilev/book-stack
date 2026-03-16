namespace BookStack.Features.Orders.Service;

using Common;
using Infrastructure.Services.Result;
using Infrastructure.Services.ServiceLifetimes;
using Models;
using Shared;

public interface IOrderService : IScopedService
{
    Task<ResultWith<CreateOrderResultServiceModel>> Create(
        CreateOrderServiceModel model,
        CancellationToken cancellationToken = default);

    Task<PaginatedModel<OrderServiceModel>> Mine(
        OrderFilterServiceModel filter,
        CancellationToken cancellationToken = default);

    Task<OrderServiceModel?> Details(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<PaginatedModel<SellerOrderServiceModel>> Sold(
        OrderFilterServiceModel filter,
        CancellationToken cancellationToken = default);

    Task<SellerOrderServiceModel?> SoldDetails(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<PaginatedModel<OrderServiceModel>> All(
        OrderFilterServiceModel filter,
        CancellationToken cancellationToken = default);

    Task<Result> ChangeStatus(
        Guid orderId,
        OrderStatus orderStatus,
        CancellationToken cancellationToken = default);

    Task<Result> ChangePaymentStatus(
        Guid orderId,
        PaymentStatus paymentStatus,
        CancellationToken cancellationToken = default);
}
