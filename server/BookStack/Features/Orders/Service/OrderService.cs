namespace BookStack.Features.Orders.Service;

using BookStack.Data;
using BookStack.Features.BookListings.Data.Models;
using Common;
using Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.PageClamper;
using Infrastructure.Services.Result;
using Microsoft.EntityFrameworkCore;
using Models;
using Shared;

public class OrderService(
    BookStackDbContext data,
    ICurrentUserService userService,
    IPageClamper pageClamper,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly BookStackDbContext _data = data;
    private readonly ICurrentUserService _userService = userService;
    private readonly IPageClamper _pageClamper = pageClamper;
    private readonly ILogger<OrderService> _logger = logger;

    public async Task<ResultWith<Guid>> Create(
        CreateOrderServiceModel model,
        CancellationToken cancellationToken = default)
    {
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

        var firstCurrency = listings[0].Currency;
        var differentCurrencyExists = listings.Any(l => l.Currency != firstCurrency);

        if (differentCurrencyExists)
        {
            return "All ordered listings must have the same currency.";
        }

        foreach (var item in items)
        {
            var listing = listings.Single(l => l.Id == item.ListingId);

            if (listing.Quantity < item.Quantity)
            {
                return $"Listing with Id: {listing.Id} does not have enough quantity.";
            }
        }

        var buyerId = this._userService.GetId();

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
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Unpaid,
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

        this._logger.LogInformation(
            "Order created. OrderId={OrderId}, BuyerId={BuyerId}, Email={Email}, ItemsCount={ItemsCount}, TotalAmount={TotalAmount}, Currency={Currency}",
            order.Id,
            buyerId,
            order.Email,
            orderItems.Count,
            order.TotalAmount,
            order.Currency);

        return ResultWith<Guid>.Success(order.Id);
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

    public async Task<OrderServiceModel?> Details(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = this._userService.GetId();
        var isAdmin = this._userService.IsAdmin();

        var query = this._data
            .Orders
            .AsNoTracking()
            .Where(o => o.Id == id);

        if (!isAdmin)
        {
            query = query.Where(o => o.BuyerId == currentUserId);
        }

        return await query
            .ToServiceModels()
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
        Guid id,
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        var order = await this._data
            .Orders
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                o => o.Id == id,
                cancellationToken);

        if (order is null || order.IsDeleted)
        {
            return string.Format(
                Common.Constants.ErrorMessages.DbEntityNotFound,
                nameof(OrderDbModel),
                id);
        }

        order.Status = status;

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Order status changed. OrderId={OrderId}, Status={Status}",
            id,
            status);

        return true;
    }

    public async Task<Result> ChangePaymentStatus(
        Guid id,
        PaymentStatus paymentStatus,
        CancellationToken cancellationToken = default)
    {
        var order = await this._data
            .Orders
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                o => o.Id == id,
                cancellationToken);

        if (order is null || order.IsDeleted)
        {
            return string.Format(
                Common.Constants.ErrorMessages.DbEntityNotFound,
                nameof(OrderDbModel),
                id);
        }

        order.PaymentStatus = paymentStatus;

        await this._data.SaveChangesAsync(cancellationToken);

        this._logger.LogInformation(
            "Order payment status changed. OrderId={OrderId}, PaymentStatus={PaymentStatus}",
            id,
            paymentStatus);

        return true;
    }

    private static IQueryable<OrderDbModel> ApplyFilter(
        IQueryable<OrderDbModel> query,
        OrderFilterServiceModel filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.BuyerId))
        {
            query = query.Where(o => o.BuyerId == filter.BuyerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            query = query.Where(o => EF.Functions.Like(o.Email, $"%{filter.Email}%"));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.PaymentStatus.HasValue)
        {
            query = query.Where(o => o.PaymentStatus == filter.PaymentStatus.Value);
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
