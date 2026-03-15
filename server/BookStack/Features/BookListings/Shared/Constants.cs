namespace BookStack.Features.BookListings.Shared;

public static class Constants
{
	public static class Validation
	{
		public const decimal MinPrice = 0.01m;
		public const decimal MaxPrice = 100_000m;

		public const int MinQuantity = 1;
		public const int MaxQuantity = 100;

		public const int DescriptionMinLength = 10;
		public const int DescriptionMaxLength = 4_000;

		public const int RejectionReasonMaxLength = 1_000;

		public const int CurrencyMinLength = 3;
		public const int CurrencyMaxLength = 3;

		public const int MinLookupSize = 1;
		public const int MaxLookupSize = 50;
	}

	public static class Pagination
	{
		public const int MaxPageSize = 100;
	}

	public static class Paths
	{
		public const string ListingsImagePathPrefix = "listings";
		public const string DefaultImagePath = $"/images/{ListingsImagePathPrefix}/default.jpg";
	}
}
