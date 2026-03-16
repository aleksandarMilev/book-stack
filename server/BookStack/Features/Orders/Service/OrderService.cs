namespace BookStack.Features.Orders.Service;

using BookStack.Data;
using Common;
using Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.DateTimeProvider;
using Infrastructure.Services.PageClamper;
using Infrastructure.Services.Result;
using Microsoft.EntityFrameworkCore;
using Models;
using Payments.Service;
using Shared;

public class OrderService(
    BookStackDbContext data,
    ICurrentUserService userService,
    IPaymentService paymentService,
    IDateTimeProvider dateTimeProvider,
    IPageClamper pageClamper,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly BookStackDbContext _data = data;
    private readonly ICurrentUserService _userService = userService;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IPageClamper _pageClamper = pageClamper;
    private readonly ILogger<OrderService> _logger = logger;

    public async Task<ResultWith<CreateOrderResultServiceModel>> Create(
        CreateOrderServiceModel model,
        CancellationToken cancellationToken = default)
    {
        await this._paymentService.ReleaseExpiredReservations(cancellationToken);

        var items = model.Items
            .Where(static i => i.Quantity > 0)
            .ToList();

        if (items.Count == 0)
        {
            return "Order must contain at least one item.";
        }

        var listingIds = items
            .Select(static i => i.ListingId)
            .Distinct()
            .ToList();

        if (listingIds.Count != items.Count)
        {
            return "Duplicate listings are not allowed in the same order.";
        }

        await using var transaction = await this
            ._data
            .Database
            .BeginTransactionAsync(cancellationToken);

        var listings = await this._data
            .BookListings
            .IgnoreQueryFilters()
            .Include(static l => l.Book)
            .Where(l => listingIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        if (listings.Count != listingIds.Count)
        {
            return "One or more selected listings were not found.";
        }

        if (listings.Any(static l => l.IsDeleted))
        {
            return "One or more selected listings are no longer available.";
        }

        if (listings.Any(static l => !l.IsApproved))
        {
            return "One or more selected listings are not approved.";
        }

        if (listings.Any(static l => l.Book.IsDeleted || !l.Book.IsApproved))
        {
            return "One or more selected books are not available for ordering.";
        }

        if (listings.Any(static l => l.Quantity <= 0))
        {
            return "One or more selected listings are out of stock.";
        }

        var firstCurrency = listings[0].Currency;
        var differentCurrencyExists = listings.Any(l => l.Currency != firstCurrency);

        if (differentCurrencyExists)
        {
            return "All ordered listings must have the same currency.";
        }

        foreach (var item in items)
        {
            var listing = listings
                .Single(l => l.Id == item.ListingId);

            if (listing.Quantity < item.Quantity)
            {
                return $"Listing with Id: {listing.Id} does not have enough quantity.";
            }
        }

        var buyerId = this._userService.GetId();
        var reservationExpiresOnUtc = this._dateTimeProvider
            .UtcNow
            .AddMinutes(Shared.Constants.Reservation.DefaultDurationMinutes);

        string? paymentToken = null;
        string? guestPaymentTokenHash = null;

        if (string.IsNullOrWhiteSpace(buyerId))
        {
            paymentToken = OrderPaymentToken.Generate();
            guestPaymentTokenHash = OrderPaymentToken.Hash(paymentToken);
        }

        var order = new OrderDbModel
        {
            BuyerId = buyerId,
            CustomerFirstName = model.CustomerFirstName.Trim(),
            CustomerLastName = model.CustomerLastName.Trim(),
            Email = model.Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null
                : model.PhoneNumber.Trim(),
            Country = model.Country.Trim(),
            City = model.City.Trim(),
            AddressLine = model.AddressLine.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(model.PostalCode)
                ? null
                : model.PostalCode.Trim(),
            Currency = firstCurrency,
            Status = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Unpaid,
            GuestPaymentTokenHash = guestPaymentTokenHash,
            ReservationExpiresOnUtc = reservationExpiresOnUtc,
            ReservationReleasedOnUtc = null,
        };

        var orderItems = new List<OrderItemDbModel>();
        decimal totalAmount = 0m;

        foreach (var item in items)
        {
            var listing = listings.Single(l => l.Id == item.ListingId);
            var lineTotal = listing.Price * item.Quantity;

            listing.Quantity -= item.Quantity;

            var orderItem = new OrderItemDbModel
            {
                Order = order,
                ListingId = listing.Id,
                BookId = listing.BookId,
                SellerId = listing.CreatorId,
                BookTitle = listing.Book.Title,
                BookAuthor = listing.Book.Author,
                BookGenre = listing.Book.Genre,
                BookPublisher = listing.Book.Publisher,
                BookPublishedOn = listing.Book.PublishedOn.HasValue
                    ? listing.Book.PublishedOn.Value.ToString(Common.Constants.DateFormats.ISO8601)
                    : null,
                BookIsbn = listing.Book.Isbn,
                UnitPrice = listing.Price,
                Quantity = item.Quantity,
                TotalPrice = lineTotal,
                Currency = listing.Currency,
                Condition = listing.Condition,
                ListingDescription = listing.Description,
                ListingImagePath = listing.ImagePath,
            };

            orderItems.Add(orderItem);
            totalAmount += lineTotal;
        }

        order.TotalAmount = totalAmount;

        this._data.Add(order);
        this._data.AddRange(orderItems);

        await this._data.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        this._logger.LogInformation(
            "Order created with reserved stock. OrderId={OrderId}, BuyerId={BuyerId}, Email={Email}, ItemsCount={ItemsCount}, TotalAmount={TotalAmount}, Currency={Currency}, ReservationExpiresOnUtc={ReservationExpiresOnUtc}",
            order.Id,
            buyerId,
            order.Email,
            orderItems.Count,
            order.TotalAmount,
            order.Currency,
            order.ReservationExpiresOnUtc);

        var resultWith = new CreateOrderResultServiceModel
        {
            OrderId = order.Id,
            PaymentToken = paymentToken,
        };

        return ResultWith<CreateOrderResultServiceModel>.Success(resultWith);
    }

    public async Task<PaginatedModel<OrderServiceModel>> Mine(
        OrderFilterServiceModel filter,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (currentUserId is null)
        {
            return new PaginatedModel<OrderServiceModel>(
                [],
                0,
                filter.PageIndex,
                filter.PageSize);
        }

        var pageIndex = filter.PageIndex;
        var pageSize = filter.PageSize;

        this._pageClamper.ClampPageSizeAndIndex(
            ref pageIndex,
            ref pageSize,
            Shared.Constants.Pagination.MaxPageSize);

        filter = new()
        {
            SearchTerm = filter.SearchTerm,
            BuyerId = currentUserId,
            Email = filter.Email,
            Status = filter.Status,
            PaymentStatus = filter.PaymentStatus,
            PageIndex = pageIndex,
            PageSize = pageSize,
        };

        var query = ApplyFilter(
            this._data.Orders.AsNoTracking(),
            filter);

        query = query.OrderByDescending(static o => o.CreatedOn);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToServiceModels()
            .ToListAsync(cancellationToken);

        return new PaginatedModel<OrderServiceModel>(
            items,
            totalItems,
            pageIndex,
            pageSize);
    }

    public async Task<PaginatedModel<SellerOrderServiceModel>> Sold(
        OrderFilterServiceModel filter,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (currentUserId is null)
        {
            return new PaginatedModel<SellerOrderServiceModel>(
                [],
                0,
                filter.PageIndex,
                filter.PageSize);
        }

        var pageIndex = filter.PageIndex;
        var pageSize = filter.PageSize;

        this._pageClamper.ClampPageSizeAndIndex(
            ref pageIndex,
            ref pageSize,
            Shared.Constants.Pagination.MaxPageSize);

        filter = new()
        {
            SearchTerm = filter.SearchTerm,
            BuyerId = null,
            Email = filter.Email,
            Status = filter.Status,
            PaymentStatus = filter.PaymentStatus,
            PageIndex = pageIndex,
            PageSize = pageSize,
        };

        var queryFilter = this._data
            .Orders
            .AsNoTracking()
            .Where(o => o
                .Items
                .Any(i => !i.IsDeleted && i.SellerId == currentUserId));

        var query = ApplyFilter(
            queryFilter,
            filter);

        query = query
            .OrderByDescending(static o => o.CreatedOn);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToSellerServiceModels(currentUserId)
            .ToListAsync(cancellationToken);

        return new PaginatedModel<SellerOrderServiceModel>(
            items,
            totalItems,
            pageIndex,
            pageSize);
    }

    public async Task<OrderServiceModel?> Details(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        var isAdmin = this._userService.IsAdmin();

        var query = this._data
            .Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId);

        if (!isAdmin)
        {
            query = query.Where(o => o.BuyerId == currentUserId);
        }

        return await query
            .ToServiceModels()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SellerOrderServiceModel?> SoldDetails(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        if (currentUserId is null)
        {
            return null;
        }

        var query = this._data
            .Orders
            .AsNoTracking()
            .Where(o =>
                o.Id == orderId &&
                o
                    .Items
                    .Any(i => !i.IsDeleted && i.SellerId == currentUserId));

        return await query
            .ToSellerServiceModels(currentUserId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedModel<OrderServiceModel>> All(
        OrderFilterServiceModel filter,
        CancellationToken cancellationToken = default)
    {
        var pageIndex = filter.PageIndex;
        var pageSize = filter.PageSize;

        this._pageClamper.ClampPageSizeAndIndex(
            ref pageIndex,
            ref pageSize,
            Shared.Constants.Pagination.MaxPageSize);

        var query = ApplyFilter(
            this._data.Orders.AsNoTracking(),
            filter);

        query = query.OrderByDescending(static o => o.CreatedOn);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToServiceModels()
            .ToListAsync(cancellationToken);

        return new PaginatedModel<OrderServiceModel>(
            items,
            totalItems,
            pageIndex,
            pageSize);
    }

    public async Task<Result> ChangeStatus(
        Guid orderId,
        OrderStatus orderStatus,
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

        order.Status = orderStatus;

        await using var transaction = await this._data
            .Database
            .BeginTransactionAsync(cancellationToken);

        await this._data.SaveChangesAsync(cancellationToken);

        if (orderStatus == OrderStatus.Cancelled && !order.IsDeleted)
        {
            var releaseResult = await this._paymentService
                .ReleaseOrderReservation(orderId, cancellationToken);

            if (!releaseResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return releaseResult.ErrorMessage!;
            }
        }

        await transaction.CommitAsync(cancellationToken);

        this._logger.LogInformation(
            "Order status changed. OrderId={OrderId}, Status={Status}",
            orderId,
            orderStatus);

        return true;
    }

    public async Task<Result> ChangePaymentStatus(
        Guid orderId,
        PaymentStatus paymentStatus,
        CancellationToken cancellationToken = default)
        => await this._paymentService.ApplyManualPaymentStatus(
            orderId,
            paymentStatus,
            cancellationToken);

    private static IQueryable<OrderDbModel> ApplyFilter(
        IQueryable<OrderDbModel> query,
        OrderFilterServiceModel filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.BuyerId))
        {
            query = query
                .Where(o => o.BuyerId == filter.BuyerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            query = query
                .Where(o => EF.Functions.Like(o.Email, $"%{filter.Email}%"));
        }

        if (filter.Status.HasValue)
        {
            query = query
                .Where(o => o.Status == filter.Status.Value);
        }

        if (filter.PaymentStatus.HasValue)
        {
            query = query
                .Where(o => o.PaymentStatus == filter.PaymentStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            query = query.Where(o =>
                EF.Functions.Like(o.CustomerFirstName, $"%{filter.SearchTerm}%") ||
                EF.Functions.Like(o.CustomerLastName, $"%{filter.SearchTerm}%") ||
                EF.Functions.Like(o.Email, $"%{filter.SearchTerm}%") ||
                EF.Functions.Like(o.City, $"%{filter.SearchTerm}%") ||
                EF.Functions.Like(o.Country, $"%{filter.SearchTerm}%"));
        }

        return query;
    }
}
