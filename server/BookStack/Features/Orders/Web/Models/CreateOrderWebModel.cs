namespace BookStack.Features.Orders.Web.Models;

using System.ComponentModel.DataAnnotations;

using static Shared.Constants.Validation;

public class CreateOrderWebModel
{
    [Required]
    [StringLength(NameMaxLength, MinimumLength = NameMinLength)]
    public string CustomerFirstName { get; init; } = default!;

    [Required]
    [StringLength(NameMaxLength, MinimumLength = NameMinLength)]
    public string CustomerLastName { get; init; } = default!;

    [Required]
    [EmailAddress]
    [StringLength(EmailMaxLength, MinimumLength = EmailMinLength)]
    public string Email { get; init; } = default!;

    [StringLength(PhoneMaxLength, MinimumLength = PhoneMinLength)]
    public string? PhoneNumber { get; init; }

    [Required]
    [StringLength(CountryMaxLength, MinimumLength = CountryMinLength)]
    public string Country { get; init; } = default!;

    [Required]
    [StringLength(CityMaxLength, MinimumLength = CityMinLength)]
    public string City { get; init; } = default!;

    [Required]
    [StringLength(AddressMaxLength, MinimumLength = AddressMinLength)]
    public string AddressLine { get; init; } = default!;

    [StringLength(PostalCodeMaxLength, MinimumLength = PostalCodeMinLength)]
    public string? PostalCode { get; init; }

    [Required]
    [MinLength(MinItemsCount)]
    [MaxLength(MaxItemsCount)]
    public IEnumerable<CreateOrderItemWebModel> Items { get; init; } = [];
}
