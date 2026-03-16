namespace BookStack.Tests.TestInfrastructure;

using BookStack.Features.Books.Service;
using BookStack.Features.Orders.Service;
using BookStack.Features.Payments.Service;
using BookStack.Features.Payments.Service.Providers;
using BookStack.Infrastructure.Services.PageClamper;
using BookStack.Infrastructure.Services.StringSanitizer;
using BookStack.Infrastructure.Settings;
using BookStack.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

internal static class TestServiceFactory
{
    public static BookService CreateBookService(
        BookStackDbContext data,
        TestCurrentUserService currentUserService,
        TestDateTimeProvider dateTimeProvider)
        => new(
            data,
            currentUserService,
            new PageClamper(),
            new StringSanitizerService(),
            dateTimeProvider,
            NullLogger<BookService>.Instance);

    public static PaymentService CreatePaymentService(
        BookStackDbContext data,
        TestDateTimeProvider dateTimeProvider,
        TestCurrentUserService currentUserService)
    {
        var appUrlSettings = new AppUrlsSettings
        {
            ClientBaseUrl = "https://bookstack.test",
        };

        var appUrlsOptions = Options.Create(appUrlSettings);

        IPaymentProvider paymentProvider = new MockPaymentProvider(
            appUrlsOptions,
            dateTimeProvider);

        IPaymentProviderRegistry paymentProviderRegistry = new PaymentProviderRegistry([paymentProvider]);

        return new(
            data,
            dateTimeProvider,
            currentUserService,
            paymentProviderRegistry,
            NullLogger<PaymentService>.Instance);
    }

    public static OrderService CreateOrderService(
        BookStackDbContext data,
        TestCurrentUserService currentUserService,
        PaymentService paymentService,
        TestDateTimeProvider dateTimeProvider)
        => new(
            data,
            currentUserService,
            paymentService,
            dateTimeProvider,
            new PageClamper(),
            NullLogger<OrderService>.Instance);
}
