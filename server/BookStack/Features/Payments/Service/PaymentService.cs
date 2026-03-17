namespace BookStack.Features.Payments.Service;

using BookStack.Data;
using Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.DateTimeProvider;
using Infrastructure.Services.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Models;
using Orders.Data.Models;
using Orders.Shared;
using Shared;

public class PaymentService(
    BookStackDbContext data,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService,
    IPaymentProviderRegistry paymentProviderRegistry,
    ILogger<PaymentService> logger) : IPaymentService
{
    private readonly BookStackDbContext _data = data;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IPaymentProviderRegistry _paymentProviderRegistry = paymentProviderRegistry;
    private readonly ILogger<PaymentService> _logger = logger;

    public async Task<ResultWith<PaymentCheckoutSessionServiceModel>> CreateCheckoutSession(
        Guid orderId,
        CreatePaymentSessionServiceModel model,
        CancellationToken cancellationToken = default)
    {
        await this.ReleaseExpiredReservations(cancellationToken);

        var providerName = NormalizeProviderName(model.Provider);
        if (!this._paymentProviderRegistry.TryGetProvider(providerName, out var paymentProvider))
        {
            return $"Payment provider '{providerName}' is not supported.";
        }

        var order = await this._data
            .Orders
            .SingleOrDefaultAsync(
                o => o.Id == orderId,
                cancellationToken);

        var isNullOrDeleted = order is null || order.IsDeleted;
        if (isNullOrDeleted)
        {
            return string.Format(
                Common.Constants.ErrorMessages.DbEntityNotFound,
                nameof(OrderDbModel),
                orderId);
        }

        if (order!.PaymentMethod != OrderPaymentMethod.Online)
        {
            return "Checkout session can only be created for online payment orders.";
        }

        var canInitiateCheckout = !this.CanInitiateCheckout(
            order!,
            model.PaymentToken);

        if (canInitiateCheckout)
        {
            return "You are not authorized to initiate payment for this order.";
        }

        var isAlreadyFinalized = order!.PaymentStatus
            is PaymentStatus.Paid 
            or PaymentStatus.Refunded;

        if (isAlreadyFinalized)
        {
            return "Order payment is already finalized.";
        }

        var isNoLongerActive = order.Status
            is OrderStatus.Cancelled
            or OrderStatus.Expired ||
            order.ReservationReleasedOnUtc.HasValue;

        if (isNoLongerActive)
        {
            return "Order reservation is no longer active.";
        }

        var hasExpired = order.ReservationExpiresOnUtc <= this._dateTimeProvider.UtcNow;
        if (hasExpired)
        {
            await this.ReleaseOrderReservationInternal(
                order.Id,
                ReservationReleaseReason.Expired,
                cancellationToken);

            return "Order reservation has expired.";
        }

        var providerRequest = new PaymentProviderCheckoutRequestServiceModel
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Currency = order.Currency,
            Email = order.Email,
        };

        var providerResult = await paymentProvider.CreateCheckoutSession(
            providerRequest,
            cancellationToken);

        if (!providerResult.Succeeded)
        {
            return providerResult.ErrorMessage ?? "Unable to create payment session.";
        }

        var providerSession = providerResult.Data!;
        var payment = new PaymentDbModel
        {
            OrderId = order.Id,
            Provider = providerName,
            ProviderPaymentId = providerSession.ProviderPaymentId,
            Amount = order.TotalAmount,
            Currency = order.Currency,
            Status = PaymentRecordStatus.Pending,
            LastEventOnUtc = this._dateTimeProvider.UtcNow,
        };

        this._data.Payments.Add(payment);

        await this._data.SaveChangesAsync(cancellationToken);

        var reconciledPaymentStatus = await this.ReconcileOrderPaymentStatus(
            order.Id,
            cancellationToken);

        await this.HandleReservationAfterPaymentReconciliation(
            order.Id,
            reconciledPaymentStatus,
            cancellationToken);

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Payment session created. PaymentId={PaymentId}, OrderId={OrderId}, Provider={Provider}, ProviderPaymentId={ProviderPaymentId}",
            payment.Id,
            order.Id,
            providerName,
            payment.ProviderPaymentId);

        var resultWith = new PaymentCheckoutSessionServiceModel
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Provider = payment.Provider,
            ProviderPaymentId = payment.ProviderPaymentId,
            CheckoutUrl = providerSession.CheckoutUrl,
            Status = payment.Status,
        };

        return ResultWith<PaymentCheckoutSessionServiceModel>
            .Success(resultWith);
    }

    public async Task<Result> ProcessWebhook(
        string provider,
        string payload,
        IHeaderDictionary headers,
        CancellationToken cancellationToken = default)
    {
        var providerName = NormalizeProviderName(provider);
        if (!this._paymentProviderRegistry.TryGetProvider(providerName, out var paymentProvider))
        {
            return $"Payment provider '{providerName}' is not supported.";
        }

        var parsedEvent = paymentProvider.ParseWebhook(payload, headers);
        if (!parsedEvent.Succeeded)
        {
            return parsedEvent.ErrorMessage ?? "Invalid webhook event.";
        }

        var webhookEventModel = parsedEvent.Data!;
        var processedOnUtc = this._dateTimeProvider.UtcNow;

        await using var transaction = await this._data
            .Database
            .BeginTransactionAsync(cancellationToken);

        var webhookEvent = new PaymentWebhookEventDbModel
        {
            Provider = providerName,
            ProviderEventId = webhookEventModel.ProviderEventId,
            ProviderPaymentId = webhookEventModel.ProviderPaymentId,
            Status = webhookEventModel.Status,
            FailureReason = webhookEventModel.FailureReason,
            ProcessedOnUtc = processedOnUtc,
            ProcessingResult = "Received",
            Payload = TrimPayload(payload),
        };

        this._data.Add(webhookEvent);

        try
        {
            await this._data.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            this._logger.LogInformation(
                "Duplicate webhook ignored. Provider={Provider}, EventId={EventId}",
                providerName,
                webhookEventModel.ProviderEventId);

            return true;
        }

        var payment = await this._data
            .Payments
            .SingleOrDefaultAsync(
                p => p.Provider == providerName &&
                     p.ProviderPaymentId == webhookEventModel.ProviderPaymentId,
                cancellationToken);

        if (payment is null || payment.IsDeleted)
        {
            webhookEvent.ProcessingResult = "PaymentNotFound";

            await this._data.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            this._logger.LogWarning(
                "Webhook payment not found. Provider={Provider}, ProviderPaymentId={ProviderPaymentId}, EventId={EventId}",
                providerName,
                webhookEventModel.ProviderPaymentId,
                webhookEventModel.ProviderEventId);

            return true;
        }

        ApplyWebhookStatus(payment, webhookEventModel);

        webhookEvent.PaymentId = payment.Id;
        webhookEvent.OrderId = payment.OrderId;
        webhookEvent.Status = payment.Status;
        webhookEvent.FailureReason = payment.FailureReason;
        webhookEvent.ProcessingResult = "Processed";

        await this._data.SaveChangesAsync(cancellationToken);

        var reconciledPaymentStatus = await this.ReconcileOrderPaymentStatus(
            payment.OrderId,
            cancellationToken);

        await this.HandleReservationAfterPaymentReconciliation(
            payment.OrderId,
            reconciledPaymentStatus,
            cancellationToken);

        await this._data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        this._logger.LogInformation(
            "Webhook processed. Provider={Provider}, EventId={EventId}, PaymentId={PaymentId}, OrderId={OrderId}, Status={Status}",
            providerName,
            webhookEventModel.ProviderEventId,
            payment.Id,
            payment.OrderId,
            payment.Status);

        return true;
    }

    public async Task<Result> ApplyManualPaymentStatus(
        Guid orderId,
        PaymentStatus paymentStatus,
        CancellationToken cancellationToken = default)
    {
        var order = await this._data
            .Orders
            .SingleOrDefaultAsync(
                o => o.Id == orderId,
                cancellationToken);

        if (order is null || order.IsDeleted)
        {
            return string.Format(
                Common.Constants.ErrorMessages.DbEntityNotFound,
                nameof(OrderDbModel),
                orderId);
        }

        if (order.PaymentMethod == OrderPaymentMethod.CashOnDelivery)
        {
            return "Cash-on-delivery orders do not support online payment status updates.";
        }

        if (paymentStatus is PaymentStatus.NotRequired or PaymentStatus.Expired)
        {
            return $"Payment status '{paymentStatus}' cannot be set manually.";
        }

        var manualPaymentStatus = paymentStatus switch
        {
            PaymentStatus.Paid => PaymentRecordStatus.Succeeded,
            PaymentStatus.Failed => PaymentRecordStatus.Failed,
            PaymentStatus.Refunded => PaymentRecordStatus.Refunded,
            PaymentStatus.Cancelled => PaymentRecordStatus.Canceled,
            _ => PaymentRecordStatus.Pending,
        };

        var manualReason = paymentStatus switch
        {
            PaymentStatus.Failed => "Manual payment override by administrator.",
            PaymentStatus.Refunded => "Manual refund override by administrator.",
            PaymentStatus.Cancelled => "Manual payment cancellation override by administrator.",
            _ => null,
        };

        var payment = new PaymentDbModel
        {
            OrderId = order.Id,
            Provider = Shared.Constants.Providers.ManualAdmin,
            ProviderPaymentId = $"manual_{Guid.NewGuid():N}",
            Amount = order.TotalAmount,
            Currency = order.Currency,
            Status = manualPaymentStatus,
            FailureReason = manualReason,
            LastProviderEventId = null,
            LastEventOnUtc = this._dateTimeProvider.UtcNow,
        };

        ApplyTerminalTimestamps(
            payment,
            manualPaymentStatus,
            payment.LastEventOnUtc.Value);

        this._data.Payments.Add(payment);

        await this._data.SaveChangesAsync(cancellationToken);

        var reconciledPaymentStatus = await this.ReconcileOrderPaymentStatus(
            order.Id,
            cancellationToken);

        await this.HandleReservationAfterPaymentReconciliation(
            order.Id,
            reconciledPaymentStatus,
            cancellationToken);

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogWarning(
            "Manual payment override applied. OrderId={OrderId}, RequestedStatus={RequestedStatus}, RecordedStatus={RecordedStatus}, User={User}",
            orderId,
            paymentStatus,
            manualPaymentStatus,
            this._currentUserService.GetUsername());

        return true;
    }

    public async Task ReleaseExpiredReservations(
        CancellationToken cancellationToken = default)
    {
        var utcNow = this._dateTimeProvider.UtcNow;

        var expiredOrderIds = await this._data
            .Orders
            .AsNoTracking()
            .Where(o =>
                !o.IsDeleted &&
                o.Status == OrderStatus.PendingPayment &&
                o.PaymentStatus == PaymentStatus.Pending &&
                o.ReservationReleasedOnUtc == null &&
                o.ReservationExpiresOnUtc <= utcNow)
            .Select(static o => o.Id)
            .ToListAsync(cancellationToken);

        foreach (var orderId in expiredOrderIds)
        {
            await this.ReleaseOrderReservationInternal(
                orderId,
                ReservationReleaseReason.Expired,
                cancellationToken);
        }
    }

    public async Task<Result> ReleaseOrderReservation(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var orderExists = await this._data
            .Orders
            .AsNoTracking()
            .AnyAsync(
                o => o.Id == orderId && !o.IsDeleted,
                cancellationToken);

        if (!orderExists)
        {
            return string.Format(
                Common.Constants.ErrorMessages.DbEntityNotFound,
                nameof(OrderDbModel),
                orderId);
        }

        await this.ReleaseOrderReservationInternal(
            orderId,
            ReservationReleaseReason.OrderCanceled,
            cancellationToken);

        return true;
    }

    public async Task<Result> ExpireOrderReservation(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await this._data
            .Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                o => o.Id == orderId && !o.IsDeleted,
                cancellationToken);

        if (order is null)
        {
            return string.Format(
                Common.Constants.ErrorMessages.DbEntityNotFound,
                nameof(OrderDbModel),
                orderId);
        }

        if (order.PaymentMethod != OrderPaymentMethod.Online)
        {
            return "Only online payment orders can expire.";
        }

        await this.ReleaseOrderReservationInternal(
            orderId,
            ReservationReleaseReason.Expired,
            cancellationToken);

        return true;
    }

    private async Task<PaymentStatus?> ReconcileOrderPaymentStatus(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await this._data
            .Orders
            .SingleOrDefaultAsync(
                o => o.Id == orderId,
                cancellationToken);

        var isNullOrDeleted = order is null || order.IsDeleted;
        if (isNullOrDeleted)
        {
            return null;
        }

        if (order!.PaymentMethod == OrderPaymentMethod.CashOnDelivery)
        {
            if (order.PaymentStatus != PaymentStatus.NotRequired)
            {
                order.PaymentStatus = PaymentStatus.NotRequired;
            }

            return PaymentStatus.NotRequired;
        }

        var paymentStatuses = await this._data
            .Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .Select(static p => p.Status)
            .ToListAsync(cancellationToken);

        var reconciledPaymentStatus = MapToOrderPaymentStatus(paymentStatuses);

        if (order!.PaymentStatus != reconciledPaymentStatus)
        {
            order.PaymentStatus = reconciledPaymentStatus;

            this._logger.LogInformation(
                "Order payment status reconciled. OrderId={OrderId}, PaymentStatus={PaymentStatus}",
                orderId,
                reconciledPaymentStatus);
        }

        var isSuccessfullyPaid =
            reconciledPaymentStatus == PaymentStatus.Paid &&
            order.Status == OrderStatus.PendingPayment;

        if (isSuccessfullyPaid)
        {
            order.Status = OrderStatus.PendingConfirmation;

            this._logger.LogInformation(
                "Order moved to pending confirmation after successful payment. OrderId={OrderId}",
                orderId);
        }

        return reconciledPaymentStatus;
    }

    private async Task HandleReservationAfterPaymentReconciliation(
        Guid orderId,
        PaymentStatus? paymentStatus,
        CancellationToken cancellationToken = default)
    {
        if (paymentStatus is not PaymentStatus.Failed and not PaymentStatus.Cancelled)
        {
            return;
        }

        await this.ReleaseOrderReservationInternal(
            orderId,
            ReservationReleaseReason.PaymentFailedOrCanceled,
            cancellationToken);
    }

    private async Task<bool> ReleaseOrderReservationInternal(
        Guid orderId,
        ReservationReleaseReason reason,
        CancellationToken cancellationToken)
    {
        var releasedOnUtc = this._dateTimeProvider.UtcNow;
        var targetStatus = reason == ReservationReleaseReason.Expired
            ? OrderStatus.Expired
            : OrderStatus.Cancelled;

        var paymentStatusOverride = reason switch
        {
            ReservationReleaseReason.Expired => PaymentStatus.Expired,
            ReservationReleaseReason.OrderCanceled => PaymentStatus.Cancelled,
            _ => (PaymentStatus?)null,
        };

        var startedLocalTransaction = this._data.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = this._data.Database.CurrentTransaction;

        if (startedLocalTransaction)
        {
            transaction = await this._data
                .Database
                .BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var claimedRows = await this._data
                .Orders
                .Where(o =>
                    o.Id == orderId &&
                    !o.IsDeleted &&
                    o.ReservationReleasedOnUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(o => o.ReservationReleasedOnUtc, releasedOnUtc)
                        .SetProperty(
                            o => o.Status,
                            o => o.Status == OrderStatus.Completed
                                ? OrderStatus.Completed
                                : targetStatus)
                        .SetProperty(
                            o => o.PaymentStatus,
                            o => paymentStatusOverride.HasValue &&
                                 o.PaymentMethod == OrderPaymentMethod.Online &&
                                 o.PaymentStatus == PaymentStatus.Pending
                                ? paymentStatusOverride.Value
                                : o.PaymentStatus),
                    cancellationToken);

            if (claimedRows == 0)
            {
                if (startedLocalTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return false;
            }

            var listingAdjustments = await this._data
                .OrderItems
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(i => i.OrderId == orderId)
                .GroupBy(i => i.ListingId)
                .Select(g => new
                {
                    ListingId = g.Key,
                    Quantity = g.Sum(i => i.Quantity),
                })
                .ToListAsync(cancellationToken);

            foreach (var adjustment in listingAdjustments)
            {
                await this._data
                    .BookListings
                    .IgnoreQueryFilters()
                    .Where(l => l.Id == adjustment.ListingId)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(
                                l => l.Quantity,
                                l => l.Quantity + adjustment.Quantity),
                        cancellationToken);
            }

            if (startedLocalTransaction && transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            this._logger.LogInformation(
                "Order reservation released. OrderId={OrderId}, Reason={Reason}, TargetStatus={TargetStatus}",
                orderId,
                reason,
                targetStatus);

            return true;
        }
        catch
        {
            if (startedLocalTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (startedLocalTransaction && transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private bool CanInitiateCheckout(
        OrderDbModel order,
        string? paymentToken)
    {
        if (this._currentUserService.IsAdmin())
        {
            return true;
        }

        var currentUserId = this._currentUserService.GetId();
        var orderHasBuyer = !string.IsNullOrWhiteSpace(order.BuyerId);

        if (!string.IsNullOrWhiteSpace(currentUserId) &&
            orderHasBuyer &&
            order.BuyerId == currentUserId)
        {
            return true;
        }

        if (orderHasBuyer)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(order.GuestPaymentTokenHash) ||
            string.IsNullOrWhiteSpace(paymentToken))
        {
            return false;
        }

        return OrderPaymentToken.Verify(
            paymentToken.Trim(),
            order.GuestPaymentTokenHash);
    }

    private static PaymentStatus MapToOrderPaymentStatus(
        IEnumerable<PaymentRecordStatus> statuses)
    {
        var statusList = statuses.ToList();

        if (statusList.Count == 0)
        {
            return PaymentStatus.Pending;
        }

        if (statusList.Any(static s => s == PaymentRecordStatus.Succeeded))
        {
            return PaymentStatus.Paid;
        }

        if (statusList.Any(static s => s is PaymentRecordStatus.Pending or PaymentRecordStatus.Processing))
        {
            return PaymentStatus.Pending;
        }

        if (statusList.Any(static s => s == PaymentRecordStatus.Refunded))
        {
            return PaymentStatus.Refunded;
        }

        if (statusList.Any(static s => s == PaymentRecordStatus.Canceled))
        {
            return PaymentStatus.Cancelled;
        }

        if (statusList.Any(static s => s == PaymentRecordStatus.Failed))
        {
            return PaymentStatus.Failed;
        }

        return PaymentStatus.Pending;
    }

    private static void ApplyWebhookStatus(
        PaymentDbModel payment,
        PaymentProviderWebhookEventServiceModel webhookEvent)
    {
        var nextStatus = ResolveNextStatus(
            payment.Status,
            webhookEvent.Status);

        payment.Status = nextStatus;
        payment.LastProviderEventId = webhookEvent.ProviderEventId;
        payment.LastEventOnUtc = webhookEvent.OccurredOnUtc;

        if (nextStatus is PaymentRecordStatus.Failed or PaymentRecordStatus.Canceled)
        {
            payment.FailureReason = webhookEvent.FailureReason;
        }
        else
        {
            payment.FailureReason = null;
        }

        ApplyTerminalTimestamps(
            payment,
            nextStatus,
            webhookEvent.OccurredOnUtc);
    }

    private static PaymentRecordStatus ResolveNextStatus(
        PaymentRecordStatus currentStatus,
        PaymentRecordStatus incomingStatus)
    {
        if (currentStatus == PaymentRecordStatus.Refunded)
        {
            return currentStatus;
        }

        if (incomingStatus == PaymentRecordStatus.Refunded)
        {
            return incomingStatus;
        }

        var isRegressionFromSucceeded =
            currentStatus == PaymentRecordStatus.Succeeded &&
            incomingStatus is PaymentRecordStatus.Pending or
                PaymentRecordStatus.Processing or
                PaymentRecordStatus.Failed or
                PaymentRecordStatus.Canceled;

        if (isRegressionFromSucceeded)
        {
            return currentStatus;
        }

        return incomingStatus;
    }

    private static void ApplyTerminalTimestamps(
        PaymentDbModel payment,
        PaymentRecordStatus status,
        DateTime eventOnUtc)
    {
        switch (status)
        {
            case PaymentRecordStatus.Succeeded:
                payment.CompletedOnUtc ??= eventOnUtc;
                break;
            case PaymentRecordStatus.Failed:
                payment.FailedOnUtc = eventOnUtc;
                break;
            case PaymentRecordStatus.Refunded:
                payment.RefundedOnUtc = eventOnUtc;
                break;
            case PaymentRecordStatus.Canceled:
                payment.CanceledOnUtc = eventOnUtc;
                break;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is SqlException sqlException)
        {
            return sqlException.Number is 2_601 or 2_627;
        }

        var message = exception.InnerException?.Message;
        if (!string.IsNullOrWhiteSpace(message) &&
            message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeProviderName(string? providerName)
        => string.IsNullOrWhiteSpace(providerName)
            ? Shared.Constants.Providers.Mock
            : providerName.Trim().ToLowerInvariant();

    private static string? TrimPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        var trimmedPayload = payload.Trim();

        return trimmedPayload.Length <= Shared.Constants.Validation.PayloadMaxLength
            ? trimmedPayload
            : trimmedPayload[..Shared.Constants.Validation.PayloadMaxLength];
    }
}

