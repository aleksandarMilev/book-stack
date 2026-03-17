namespace BookStack.Features.Orders.Shared;

using Data.Models;
using Service.Models;
using Web.Models;

public static class OrderMapping
{
    public static IQueryable<OrderServiceModel> ToServiceModels(
        this IQueryable<OrderDbModel> dbModels)
        => dbModels.Select(static o => new OrderServiceModel
        {
            Id = o.Id,
            BuyerId = o.BuyerId,
            CustomerFirstName = o.CustomerFirstName,
            CustomerLastName = o.CustomerLastName,
            Email = o.Email,
            PhoneNumber = o.PhoneNumber,
            Country = o.Country,
            City = o.City,
            AddressLine = o.AddressLine,
            PostalCode = o.PostalCode,
            TotalAmount = o.TotalAmount,
            Currency = o.Currency,
            PaymentMethod = o.PaymentMethod,
            Status = o.Status,
            PaymentStatus = o.PaymentStatus,
            SettlementStatus = o.SettlementStatus,
            PlatformFeePercent = o.PlatformFeePercent,
            PlatformFeeAmount = o.PlatformFeeAmount,
            SellerNetAmount = o.SellerNetAmount,
            CreatedOn = o.CreatedOn.ToString("O"),
            Items = o.Items
                .Where(static i => !i.IsDeleted)
                .Select(static i => new OrderItemServiceModel
                {
                    Id = i.Id,
                    ListingId = i.ListingId,
                    BookId = i.BookId,
                    SellerId = i.SellerId,
                    BookTitle = i.BookTitle,
                    BookAuthor = i.BookAuthor,
                    BookGenre = i.BookGenre,
                    BookPublisher = i.BookPublisher,
                    BookPublishedOn = i.BookPublishedOn,
                    BookIsbn = i.BookIsbn,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    TotalPrice = i.TotalPrice,
                    Currency = i.Currency,
                    Condition = i.Condition,
                    ListingDescription = i.ListingDescription,
                    ListingImagePath = i.ListingImagePath,
                })
                .ToList(),
        });

    public static IQueryable<SellerOrderServiceModel> ToSellerServiceModels(
        this IQueryable<OrderDbModel> dbModels,
        string sellerId)
        => dbModels.Select(o => new SellerOrderServiceModel
        {
            Id = o.Id,
            CustomerFirstName = o.CustomerFirstName,
            CustomerLastName = o.CustomerLastName,
            Email = o.Email,
            PhoneNumber = o.PhoneNumber,
            Country = o.Country,
            City = o.City,
            AddressLine = o.AddressLine,
            PostalCode = o.PostalCode,
            SellerTotalAmount = o.Items
                .Where(i => !i.IsDeleted && i.SellerId == sellerId)
                .Sum(i => i.TotalPrice),
            Currency = o.Currency,
            PaymentMethod = o.PaymentMethod,
            Status = o.Status,
            PaymentStatus = o.PaymentStatus,
            SettlementStatus = o.SettlementStatus,
            PlatformFeePercent = o.PlatformFeePercent,
            PlatformFeeAmount = o.PlatformFeeAmount,
            SellerNetAmount = o.SellerNetAmount,
            CreatedOn = o.CreatedOn.ToString("O"),
            Items = o.Items
                .Where(i => !i.IsDeleted && i.SellerId == sellerId)
                .Select(i => new SellerOrderItemServiceModel
                {
                    Id = i.Id,
                    ListingId = i.ListingId,
                    BookId = i.BookId,
                    BookTitle = i.BookTitle,
                    BookAuthor = i.BookAuthor,
                    BookGenre = i.BookGenre,
                    BookPublisher = i.BookPublisher,
                    BookPublishedOn = i.BookPublishedOn,
                    BookIsbn = i.BookIsbn,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    TotalPrice = i.TotalPrice,
                    Currency = i.Currency,
                    Condition = i.Condition,
                    ListingDescription = i.ListingDescription,
                    ListingImagePath = i.ListingImagePath,
                })
                .ToList(),
        });

    public static CreateOrderServiceModel ToCreateServiceModel(
        this CreateOrderWebModel webModel)
        => new()
        {
            CustomerFirstName = webModel.CustomerFirstName,
            CustomerLastName = webModel.CustomerLastName,
            Email = webModel.Email,
            PhoneNumber = webModel.PhoneNumber,
            Country = webModel.Country,
            City = webModel.City,
            AddressLine = webModel.AddressLine,
            PostalCode = webModel.PostalCode,
            PaymentMethod = webModel.PaymentMethod,
            Items = webModel.Items.Select(static i => new CreateOrderItemServiceModel
            {
                ListingId = i.ListingId,
                Quantity = i.Quantity,
            }),
        };

    public static CreateOrderResultWebModel ToWebModel(
        this CreateOrderResultServiceModel serviceModel)
        => new()
        {
            OrderId = serviceModel.OrderId,
            PaymentToken = serviceModel.PaymentToken,
        };

    public static OrderFilterServiceModel ToFilterServiceModel(
        this OrderFilterWebModel webModel,
        string? buyerId = null)
        => new()
        {
            SearchTerm = webModel.SearchTerm,
            BuyerId = buyerId,
            Email = webModel.Email,
            Status = webModel.Status,
            PaymentMethod = webModel.PaymentMethod,
            PaymentStatus = webModel.PaymentStatus,
            SettlementStatus = webModel.SettlementStatus,
            PageIndex = webModel.PageIndex,
            PageSize = webModel.PageSize,
        };
}
