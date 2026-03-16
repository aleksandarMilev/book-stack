import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { type BookModel,booksApi } from '@/features/books/api/books.api';

type BookSortOption = 'newest' | 'oldest' | 'titleAsc' | 'titleDesc' | 'publishedDateDesc' | 'publishedDateAsc';

const BOOK_SORT_TO_BACKEND: Record<BookSortOption, number> = {
  newest: 0,
  oldest: 1,
  titleAsc: 2,
  titleDesc: 3,
  publishedDateDesc: 4,
  publishedDateAsc: 5,
};

const isBookSortOption = (value: string): value is BookSortOption =>
  ['newest', 'oldest', 'titleAsc', 'titleDesc', 'publishedDateDesc', 'publishedDateAsc'].includes(value);

export function BooksPage() {
  const { t } = useTranslation();
  const [searchTerm, setSearchTerm] = useState('');
  const [genre, setGenre] = useState('');
  const [sortOption, setSortOption] = useState<BookSortOption>('newest');
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [books, setBooks] = useState<BookModel[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const totalPages = Math.max(1, Math.ceil(totalItems / Math.max(pageSize, 1)));

  useEffect(() => {
    let isActive = true;

    const loadBooks = async (): Promise<void> => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const response = await booksApi.getBooks({
          searchTerm: searchTerm || undefined,
          genre: genre || undefined,
          sorting: BOOK_SORT_TO_BACKEND[sortOption],
          pageIndex,
          pageSize,
          isApproved: true,
        });

        if (!isActive) {
          return;
        }

        setBooks(response.items);
        setTotalItems(response.totalItems);
      } catch (error: unknown) {
        if (!isActive) {
          return;
        }

        setBooks([]);
        setTotalItems(0);
        setErrorMessage(getApiErrorMessage(error, t('pages.books.errorDescription')));
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    void loadBooks();

    return () => {
      isActive = false;
    };
  }, [genre, pageIndex, pageSize, searchTerm, sortOption, t]);

  useEffect(() => {
    if (pageIndex <= totalPages) {
      return;
    }

    setPageIndex(totalPages);
  }, [pageIndex, totalPages]);

  const resetFilters = (): void => {
    setSearchTerm('');
    setGenre('');
    setSortOption('newest');
    setPageIndex(1);
    setPageSize(10);
  };

  return (
    <Container className="books-page">
      <header className="marketplace-header">
        <h1>{t('pages.books.title')}</h1>
        <p>{t('pages.books.subtitle')}</p>
      </header>

      <section className="marketplace-toolbar books-toolbar">
        <Input
          label={t('pages.books.searchLabel')}
          onChange={(event) => {
            setPageIndex(1);
            setSearchTerm(event.target.value);
          }}
          placeholder={t('pages.books.searchPlaceholder')}
          value={searchTerm}
        />
        <Input
          label={t('pages.books.genreLabel')}
          onChange={(event) => {
            setPageIndex(1);
            setGenre(event.target.value);
          }}
          placeholder={t('pages.books.genrePlaceholder')}
          value={genre}
        />
        <label className="marketplace-sort-label" htmlFor="books-sort">
          <span>{t('pages.books.sortLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="books-sort"
            onChange={(event) => {
              if (isBookSortOption(event.target.value)) {
                setPageIndex(1);
                setSortOption(event.target.value);
              }
            }}
            value={sortOption}
          >
            <option value="newest">{t('pages.books.sortOptions.newest')}</option>
            <option value="oldest">{t('pages.books.sortOptions.oldest')}</option>
            <option value="titleAsc">{t('pages.books.sortOptions.titleAsc')}</option>
            <option value="titleDesc">{t('pages.books.sortOptions.titleDesc')}</option>
            <option value="publishedDateDesc">{t('pages.books.sortOptions.publishedDateDesc')}</option>
            <option value="publishedDateAsc">{t('pages.books.sortOptions.publishedDateAsc')}</option>
          </select>
        </label>
        <label className="marketplace-sort-label" htmlFor="books-page-size">
          <span>{t('marketplace.pageSizeLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="books-page-size"
            onChange={(event) => {
              setPageIndex(1);
              setPageSize(Number.parseInt(event.target.value, 10));
            }}
            value={pageSize}
          >
            {[10, 20, 30].map((option) => (
              <option key={option} value={option}>
                {t('marketplace.pageSizeOption', { count: option })}
              </option>
            ))}
          </select>
        </label>
      </section>

      <div className="marketplace-results">
        <p className="marketplace-results-count">{t('pages.books.resultsCount', { count: totalItems })}</p>

        {isLoading ? (
          <LoadingState description={t('pages.books.loadingDescription')} title={t('pages.books.loadingTitle')} />
        ) : null}

        {!isLoading && errorMessage ? (
          <EmptyState
            action={<Button onClick={resetFilters}>{t('common.actions.reset')}</Button>}
            description={errorMessage}
            title={t('pages.books.errorTitle')}
          />
        ) : null}

        {!isLoading && !errorMessage && books.length === 0 ? (
          <EmptyState
            action={<Button onClick={resetFilters}>{t('common.actions.reset')}</Button>}
            description={t('pages.books.emptyDescription')}
            title={t('pages.books.emptyTitle')}
          />
        ) : null}

        {!isLoading && !errorMessage && books.length > 0 ? (
          <div className="books-grid">
            {books.map((book) => (
              <Card className="book-card" key={book.id}>
                <h3>{book.title}</h3>
                <p className="book-card-author">{book.author}</p>
                <p className="book-card-genre">{book.genre}</p>
                {book.publisher ? (
                  <p className="book-card-meta">
                    {t('pages.books.publisherLabel')}: {book.publisher}
                  </p>
                ) : null}
                {book.publishedOn ? (
                  <p className="book-card-meta">
                    {t('pages.books.publishedOnLabel')}: {book.publishedOn}
                  </p>
                ) : null}
                {book.isbn ? (
                  <p className="book-card-meta">
                    {t('pages.books.isbnLabel')}: {book.isbn}
                  </p>
                ) : null}
                {book.description ? <p className="book-card-description">{book.description}</p> : null}
              </Card>
            ))}
          </div>
        ) : null}

        {!isLoading && !errorMessage && books.length > 0 ? (
          <div className="marketplace-pagination">
            <Button
              disabled={pageIndex <= 1}
              onClick={() => {
                setPageIndex((previousPageIndex) => Math.max(previousPageIndex - 1, 1));
              }}
              variant="secondary"
            >
              {t('common.actions.previous')}
            </Button>
            <p>{t('marketplace.pageIndicator', { page: pageIndex, totalPages })}</p>
            <Button
              disabled={pageIndex >= totalPages}
              onClick={() => {
                setPageIndex((previousPageIndex) => Math.min(previousPageIndex + 1, totalPages));
              }}
              variant="secondary"
            >
              {t('common.actions.next')}
            </Button>
          </div>
        ) : null}
      </div>
    </Container>
  );
}
