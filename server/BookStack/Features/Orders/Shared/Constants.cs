namespace BookStack.Features.Orders.Shared;

public static class Constants
{
	public static class Validation
	{
		public const int NameMinLength = 2;
		public const int NameMaxLength = 100;

		public const int EmailMinLength = 5;
		public const int EmailMaxLength = 254;

		public const int PhoneMinLength = 5;
		public const int PhoneMaxLength = 30;

		public const int CountryMinLength = 2;
		public const int CountryMaxLength = 100;

		public const int CityMinLength = 2;
		public const int CityMaxLength = 100;

		public const int AddressMinLength = 5;
		public const int AddressMaxLength = 300;

		public const int PostalCodeMinLength = 2;
		public const int PostalCodeMaxLength = 20;

		public const int CurrencyMinLength = 3;
		public const int CurrencyMaxLength = 3;

		public const int MinItemsCount = 1;
		public const int MaxItemsCount = 20;

		public const int MinItemQuantity = 1;
		public const int MaxItemQuantity = 20;

		public const int BookTitleMaxLength = 300;
		public const int BookAuthorMaxLength = 200;
		public const int BookGenreMaxLength = 100;
		public const int BookPublisherMaxLength = 200;
		public const int BookIsbnMaxLength = 32;
		public const int DateStringMaxLength = 10;
		public const int ListingDescriptionMaxLength = 4_000;

		public const int MinPageSize = 1;
		public const int MaxPageSize = 100;
	}

	public static class Pagination
	{
		public const int MaxPageSize = 100;
	}
}
