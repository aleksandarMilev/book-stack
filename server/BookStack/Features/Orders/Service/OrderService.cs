namespace BookStack.Features.Orders.Service;

using BookStack.Data;
using Common;
using Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.DateTimeProvider;
using Infrastructure.Services.PageClamper;
using Infrastructure.Services.Result;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Payments.Service;
using SellerProfiles.Service;
using Shared;

public class OrderService(
    BookStackDbContext data,
    ICurrentUserService userService,
    IPaymentService paymentService,
    ISellerProfileService sellerProfileService,
    IOptions<PlatformFeeSettings> platformFeeSettings,
    IDateTimeProvider dateTimeProvider,
    IPageClamper pageClamper,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly BookStackDbContext _data = data;
    private readonly ICurrentUserService _userService = userService;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly ISellerProfileService _sellerProfileService = sellerProfileService;
    private readonly PlatformFeeSettings _platformFeeSettings = platformFeeSettings.Value;
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

        var sellerIds = listings
            .Select(static l => l.CreatorId)
            .Distinct()
            .ToList();

        if (sellerIds.Count != 1)
        {
            return "All ordered items must belong to the same seller.";
        }

        var sellerId = sellerIds[0];
        var sellerProfile = await this._sellerProfileService.ActiveByUserId(
            sellerId,
            cancellationToken);

        if (sellerProfile is null)
        {
            return "The seller is not currently active.";
        }

        var sellerSupportsPaymentMethod = model.PaymentMethod switch
        {
            OrderPaymentMethod.Online => sellerProfile.SupportsOnlinePayment,
            OrderPaymentMethod.CashOnDelivery => sellerProfile.SupportsCashOnDelivery,
            _ => false,
        };

        if (!sellerSupportsPaymentMethod)
        {
            return "The selected payment method is not supported by this seller.";
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

        if (string.IsNullOrWhiteSpace(buyerId) &&
            model.PaymentMethod == OrderPaymentMethod.Online)
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
            PaymentMethod = model.PaymentMethod,
            Status = model.PaymentMethod == OrderPaymentMethod.Online
                ? OrderStatus.PendingPayment
                : OrderStatus.PendingConfirmation,
            PaymentStatus = model.PaymentMethod == OrderPaymentMethod.Online
                ? PaymentStatus.Pending
                : PaymentStatus.NotRequired,
            SettlementStatus = SettlementStatus.Pending,
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
        order.PlatformFeePercent = GetValidPlatformFeePercent(this._platformFeeSettings);
        order.PlatformFeeAmount = CalculatePlatformFeeAmount(
            order.TotalAmount,
            order.PlatformFeePercent);
        order.SellerNetAmount = RoundMoney(order.TotalAmount - order.PlatformFeeAmount);

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
            PaymentMethod = filter.PaymentMethod,
            PaymentStatus = filter.PaymentStatus,
            SettlementStatus = filter.SettlementStatus,
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
            PaymentMethod = filter.PaymentMethod,
            PaymentStatus = filter.PaymentStatus,
            SettlementStatus = filter.SettlementStatus,
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
        if (!this._userService.IsAdmin())
        {
            return "Only administrators can change order status through this operation.";
        }

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

        return await this.ChangeOrderStatusInternal(
            order,
            orderStatus,
            OrderStatusTransitionActor.Admin,
            cancellationToken);
    }

    public async Task<Result> ChangePaymentStatus(
        Guid orderId,
        PaymentStatus paymentStatus,
        CancellationToken cancellationToken = default)
        => await this._paymentService.ApplyManualPaymentStatus(
            orderId,
            paymentStatus,
            cancellationToken);

    public async Task<Result> ChangeSettlementStatus(
        Guid orderId,
        SettlementStatus settlementStatus,
        CancellationToken cancellationToken = default)
    {
        if (!this._userService.IsAdmin())
        {
            return "Only administrators can change settlement status.";
        }

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

        var canTransitionSettlementStatus = CanTransitionSettlementStatus(
            order,
            settlementStatus);

        if (!canTransitionSettlementStatus)
        {
            if (settlementStatus == SettlementStatus.Settled &&
                !IsSettlementEligible(order))
            {
                return "Order is not eligible for settlement yet.";
            }

            return $"Settlement transition from '{order.SettlementStatus}' to '{settlementStatus}' is not allowed.";
        }

        order.SettlementStatus = settlementStatus;
        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Order settlement status changed. OrderId={OrderId}, SettlementStatus={SettlementStatus}",
            orderId,
            settlementStatus);

        return true;
    }

    public async Task<Result> ConfirmSoldOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
        => await this.ChangeSoldOrderStatus(
            orderId,
            OrderStatus.Confirmed,
            cancellationToken);

    public async Task<Result> ShipSoldOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
        => await this.ChangeSoldOrderStatus(
            orderId,
            OrderStatus.Shipped,
            cancellationToken);

    public async Task<Result> DeliverSoldOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
        => await this.ChangeSoldOrderStatus(
            orderId,
            OrderStatus.Delivered,
            cancellationToken);

    private async Task<Result> ChangeSoldOrderStatus(
        Guid orderId,
        OrderStatus targetStatus,
        CancellationToken cancellationToken)
    {
        var sellerId = this._userService.GetId();
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return Common.Constants.ErrorMessages.CurrentUserNotAuthenticated;
        }

        var hasActiveSellerProfile = await this._sellerProfileService.HasActiveProfile(
            sellerId,
            cancellationToken);

        if (!hasActiveSellerProfile)
        {
            return "An active seller profile is required to fulfill sold orders.";
        }

        var order = await this._data
            .Orders
            .Include(static o => o.Items)
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

        var sellerOwnsOrder = order
            .Items
            .Any(i => !i.IsDeleted && i.SellerId == sellerId);

        if (!sellerOwnsOrder)
        {
            return string.Format(
                Common.Constants.ErrorMessages.UnauthorizedMessage,
                sellerId,
                nameof(OrderDbModel),
                orderId);
        }

        return await this.ChangeOrderStatusInternal(
            order,
            targetStatus,
            OrderStatusTransitionActor.Seller,
            cancellationToken);
    }

    private async Task<Result> ChangeOrderStatusInternal(
        OrderDbModel order,
        OrderStatus targetStatus,
        OrderStatusTransitionActor actor,
        CancellationToken cancellationToken)
    {
        var currentStatus = order.Status;

        var canTransition = CanTransitionOrderStatus(
            currentStatus,
            targetStatus,
            order.PaymentMethod,
            order.PaymentStatus,
            actor);

        if (!canTransition)
        {
            return $"Order transition from '{order.Status}' to '{targetStatus}' is not allowed.";
        }

        if (targetStatus is OrderStatus.Cancelled or OrderStatus.Expired)
        {
            var releaseResult = targetStatus == OrderStatus.Cancelled
                ? await this._paymentService.ReleaseOrderReservation(
                    order.Id,
                    cancellationToken)
                : await this._paymentService.ExpireOrderReservation(
                    order.Id,
                    cancellationToken);

            if (!releaseResult.Succeeded)
            {
                return releaseResult.ErrorMessage!;
            }
        }
        else if (order.Status != targetStatus)
        {
            order.Status = targetStatus;
            await this._data.SaveChangesAsync(cancellationToken);
        }

        this._logger.LogInformation(
            "Order status changed. OrderId={OrderId}, From={CurrentStatus}, To={TargetStatus}, Actor={Actor}",
            order.Id,
            currentStatus,
            targetStatus,
            actor);

        return true;
    }

    private static bool CanTransitionOrderStatus(
        OrderStatus current,
        OrderStatus next,
        OrderPaymentMethod paymentMethod,
        PaymentStatus paymentStatus,
        OrderStatusTransitionActor actor)
    {
        if (current == next)
        {
            return true;
        }

        var transitionAllowedByActor = actor switch
        {
            OrderStatusTransitionActor.Admin => current switch
            {
                OrderStatus.PendingPayment => next is OrderStatus.PendingConfirmation or OrderStatus.Cancelled or OrderStatus.Expired,
                OrderStatus.PendingConfirmation => next is OrderStatus.Confirmed or OrderStatus.Cancelled,
                OrderStatus.Confirmed => next is OrderStatus.Shipped or OrderStatus.Cancelled,
                OrderStatus.Shipped => next == OrderStatus.Delivered,
                OrderStatus.Delivered => next == OrderStatus.Completed,
                _ => false,
            },
            OrderStatusTransitionActor.Seller => current switch
            {
                OrderStatus.PendingConfirmation => next == OrderStatus.Confirmed,
                OrderStatus.Confirmed => next == OrderStatus.Shipped,
                OrderStatus.Shipped => next == OrderStatus.Delivered,
                _ => false,
            },
            _ => false,
        };

        if (!transitionAllowedByActor)
        {
            return false;
        }

        if (paymentMethod == OrderPaymentMethod.CashOnDelivery &&
            current == OrderStatus.PendingPayment)
        {
            return false;
        }

        if (next == OrderStatus.Expired &&
            paymentMethod != OrderPaymentMethod.Online)
        {
            return false;
        }

        if (paymentMethod == OrderPaymentMethod.Online &&
            next == OrderStatus.PendingConfirmation &&
            paymentStatus != PaymentStatus.Paid)
        {
            return false;
        }

        var movingIntoFulfillment = next is OrderStatus.Confirmed or OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Completed;

        if (paymentMethod == OrderPaymentMethod.Online &&
            movingIntoFulfillment &&
            paymentStatus != PaymentStatus.Paid)
        {
            return false;
        }

        return true;
    }

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

        if (filter.PaymentMethod.HasValue)
        {
            query = query
                .Where(o => o.PaymentMethod == filter.PaymentMethod.Value);
        }

        if (filter.PaymentStatus.HasValue)
        {
            query = query
                .Where(o => o.PaymentStatus == filter.PaymentStatus.Value);
        }

        if (filter.SettlementStatus.HasValue)
        {
            query = query
                .Where(o => o.SettlementStatus == filter.SettlementStatus.Value);
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

    private static decimal GetValidPlatformFeePercent(
        PlatformFeeSettings settings)
    {
        if (settings.Percent < 0m || settings.Percent > 100m)
        {
            throw new InvalidOperationException(
                "Platform fee percent must be between 0 and 100.");
        }

        return settings.Percent;
    }

    private static decimal CalculatePlatformFeeAmount(
        decimal grossAmount,
        decimal platformFeePercent)
        => RoundMoney(grossAmount * platformFeePercent / 100m);

    private static decimal RoundMoney(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static bool CanTransitionSettlementStatus(
        OrderDbModel order,
        SettlementStatus next)
    {
        if (!CanTransitionSettlementStatus(
                order.SettlementStatus,
                next))
        {
            return false;
        }

        if (next == SettlementStatus.Settled &&
            !IsSettlementEligible(order))
        {
            return false;
        }

        return true;
    }

    private static bool CanTransitionSettlementStatus(
        SettlementStatus current,
        SettlementStatus next)
    {
        if (current == next)
        {
            return true;
        }

        return current switch
        {
            SettlementStatus.Pending => next is SettlementStatus.Settled or SettlementStatus.Waived or SettlementStatus.Disputed,
            SettlementStatus.Disputed => next is SettlementStatus.Settled or SettlementStatus.Waived,
            SettlementStatus.Settled => next == SettlementStatus.Disputed,
            SettlementStatus.Waived => next == SettlementStatus.Disputed,
            _ => false,
        };
    }

    private static bool IsSettlementEligible(OrderDbModel order)
    {
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Expired)
        {
            return false;
        }

        if (order.PaymentMethod == OrderPaymentMethod.Online)
        {
            return order.PaymentStatus == PaymentStatus.Paid;
        }

        return order.Status is OrderStatus.Delivered or OrderStatus.Completed;
    }

    private enum OrderStatusTransitionActor
    {
        Admin = 0,
        Seller = 1,
    }
}
