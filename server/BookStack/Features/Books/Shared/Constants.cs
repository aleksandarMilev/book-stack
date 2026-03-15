namespace BookStack.Features.Books.Shared;

public static class Constants
{
    public static class Validation
    {
        public const int TitleMinLength = 1;
        public const int TitleMaxLength = 300;

        public const int AuthorMinLength = 2;
        public const int AuthorMaxLength = 200;

        public const int GenreMinLength = 2;
        public const int GenreMaxLength = 100;

        public const int PublisherMinLength = 2;
        public const int PublisherMaxLength = 200;

        public const int DescriptionMinLength = 20;
        public const int DescriptionMaxLength = 4_000;

        public const int IsbnMinLength = 10;
        public const int IsbnMaxLength = 32;

        public const int MaxGenresCount = 20;
        public const int MaxAuthorsCount = 20;

        public const int MinLookupSize = 1;
        public const int MaxLookupSize = 50;
    }

    public static class Pagination
    {
        public const int MaxPageSize = 100;
    }
}
