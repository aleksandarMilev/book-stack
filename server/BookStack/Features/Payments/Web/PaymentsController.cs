namespace BookStack.Features.Payments.Web;

using Common;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Service;
using Service.Models;

[Authorize]
public class PaymentsController(IPaymentService paymentService) : ApiController
{
    private readonly IPaymentService _paymentService = paymentService;

    [AllowAnonymous]
    [HttpPost(ApiRoutes.Checkout)]
    public async Task<ActionResult<PaymentCheckoutSessionWebModel>> CreateCheckoutSession(
        Guid orderId,
        [FromBody] CreatePaymentSessionWebModel? webModel,
        CancellationToken cancellationToken = default)
    {
        var serviceModel = new CreatePaymentSessionServiceModel
        {
            Provider = webModel?.Provider,
            PaymentToken = webModel?.PaymentToken,
        };

        var result = await this._paymentService.CreateCheckoutSession(
            orderId,
            serviceModel,
            cancellationToken);

        return this.OkOrBadRequest(
            result,
            static payment => new PaymentCheckoutSessionWebModel
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                Provider = payment.Provider,
                ProviderPaymentId = payment.ProviderPaymentId,
                CheckoutUrl = payment.CheckoutUrl,
                Status = payment.Status,
            });
    }

    [AllowAnonymous]
    [HttpPost(ApiRoutes.Webhook)]
    public async Task<ActionResult> Webhook(
        string provider,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(this.Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        var result = await this._paymentService.ProcessWebhook(
            provider,
            payload,
            this.Request.Headers,
            cancellationToken);

        if (result.Succeeded)
        {
            return this.Ok();
        }

        return this.BadRequest(new
        {
            errorMessage = result.ErrorMessage,
        });
    }
}

