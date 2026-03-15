namespace BookStack.Features.Orders.Web.Models;

using System.ComponentModel.DataAnnotations;

using static Shared.Constants.Validation;

public class CreateOrderItemWebModel
{
    [Required]
    public Guid ListingId { get; init; }

    [Range(MinItemQuantity, MaxItemQuantity)]
    public int Quantity { get; init; }
}
