namespace BookStack.Features.Payments.Service.Providers;

using System.Text.Json;
using Infrastructure.Services.DateTimeProvider;
using Infrastructure.Services.Result;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Models;
using Shared;

public class MockPaymentProvider(
    IOptions<AppUrlsSettings> appUrlsSettings,
    IDateTimeProvider dateTimeProvider) : IPaymentProvider
{
    private sealed class MockWebhookPayload
    {
        public string EventId { get; init; } = default!;

        public string PaymentSessionId { get; init; } = default!;

        public string Status { get; init; } = default!;

        public string? FailureReason { get; init; }

        public DateTime? OccurredOnUtc { get; init; }
    }

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly AppUrlsSettings _appUrlsSettings = appUrlsSettings.Value;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public string Name
        => Constants.Providers.Mock;

    public Result ValidateWebhookSignature(
        string payload,
        IHeaderDictionary headers)
        => true;

    public Task<ResultWith<PaymentProviderCheckoutResultServiceModel>> CreateCheckoutSession(
        PaymentProviderCheckoutRequestServiceModel model,
        CancellationToken cancellationToken = default)
    {
        var providerPaymentId = $"mock_session_{Guid.NewGuid():N}";
        var baseUrl = this._appUrlsSettings.ClientBaseUrl?.TrimEnd('/');

        var checkoutUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? $"/payments/mock/checkout?sessionId={providerPaymentId}"
            : $"{baseUrl}/payments/mock/checkout?sessionId={providerPaymentId}";

        var result = new PaymentProviderCheckoutResultServiceModel
        {
            ProviderPaymentId = providerPaymentId,
            CheckoutUrl = checkoutUrl,
        };

        var resultWith = ResultWith<PaymentProviderCheckoutResultServiceModel>
            .Success(result);

        return Task.FromResult(resultWith);
    }

    public ResultWith<PaymentProviderWebhookEventServiceModel> ParseWebhook(
        string payload,
        IHeaderDictionary headers)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return "Webhook payload is empty.";
        }

        MockWebhookPayload? model;

        try
        {
            model = JsonSerializer.Deserialize<MockWebhookPayload>(
                payload,
                jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return "Webhook payload is not valid JSON.";
        }

        if (model is null)
        {
            return "Webhook payload is invalid.";
        }

        if (string.IsNullOrWhiteSpace(model.EventId))
        {
            return "Webhook event id is required.";
        }

        if (string.IsNullOrWhiteSpace(model.PaymentSessionId))
        {
            return "Webhook payment session id is required.";
        }

        if (!TryMapStatus(model.Status, out var status))
        {
            return "Webhook status is invalid.";
        }

        var occurredOnUtc = model
            .OccurredOnUtc
            ?.ToUniversalTime()
            ?? this._dateTimeProvider.UtcNow;

        var eventModel = new PaymentProviderWebhookEventServiceModel
        {
            ProviderEventId = model.EventId.Trim(),
            ProviderPaymentId = model.PaymentSessionId.Trim(),
            Status = status,
            FailureReason = string.IsNullOrWhiteSpace(model.FailureReason)
                ? null
                : model.FailureReason.Trim(),
            OccurredOnUtc = occurredOnUtc,
        };

        return ResultWith<PaymentProviderWebhookEventServiceModel>
            .Success(eventModel);
    }

    private static bool TryMapStatus(
        string? status,
        out PaymentRecordStatus mappedStatus)
    {
        mappedStatus = PaymentRecordStatus.Pending;

        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var normalized = status
            .Trim()
            .ToLowerInvariant();

        mappedStatus = normalized switch
        {
            "pending" => PaymentRecordStatus.Pending,
            "processing" => PaymentRecordStatus.Processing,
            "paid" => PaymentRecordStatus.Succeeded,
            "succeeded" => PaymentRecordStatus.Succeeded,
            "failed" => PaymentRecordStatus.Failed,
            "refunded" => PaymentRecordStatus.Refunded,
            "canceled" => PaymentRecordStatus.Canceled,
            "cancelled" => PaymentRecordStatus.Canceled,
            _ => mappedStatus,
        };

        return normalized is
            "pending" or
            "processing" or
            "paid" or
            "succeeded" or
            "failed" or
            "refunded" or
            "canceled" or
            "cancelled";
    }
}
