import type { FormEvent } from 'react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, Input, LoadingState } from '@/components/ui';
import { type BookLookupItem, booksApi } from '@/features/books/api/books.api';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import { SellerProfileRequiredState } from '@/features/sellerProfiles/components/SellerProfileRequiredState';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { ROUTES } from '@/routes/paths';
import type { MarketplaceListingCondition } from '@/types/marketplace.types';

type CreateMode = 'existingBook' | 'missingBook';

interface ListingFormState {
  price: string;
  quantity: string;
  condition: MarketplaceListingCondition;
  description: string;
  image: File | null;
}

interface BookFormState {
  title: string;
  author: string;
  genre: string;
  description: string;
  publisher: string;
  publishedOn: string;
  isbn: string;
}

const DEFAULT_CURRENCY = 'EUR';

const INITIAL_LISTING_FORM: ListingFormState = {
  price: '',
  quantity: '1',
  condition: 'veryGood',
  description: '',
  image: null,
};

const INITIAL_BOOK_FORM: BookFormState = {
  title: '',
  author: '',
  genre: '',
  description: '',
  publisher: '',
  publishedOn: '',
  isbn: '',
};

const CONDITION_OPTIONS: MarketplaceListingCondition[] = [
  'new',
  'likeNew',
  'veryGood',
  'good',
  'acceptable',
  'poor',
];

const parsePositiveNumber = (value: string): number => {
  const parsedValue = Number.parseFloat(value);
  if (Number.isNaN(parsedValue)) {
    return 0;
  }

  return parsedValue;
};

const parsePositiveInteger = (value: string): number => {
  const parsedValue = Number.parseInt(value, 10);
  if (Number.isNaN(parsedValue)) {
    return 0;
  }

  return parsedValue;
};

export function MyListingCreatePage() {
  const { t } = useTranslation();
  const sellerProfile = useSellerProfileStore((state) => state.profile);
  const sellerProfileLoadState = useSellerProfileStore((state) => state.loadState);
  const loadSellerProfile = useSellerProfileStore((state) => state.loadMine);

  const [createMode, setCreateMode] = useState<CreateMode>('existingBook');
  const [bookSearchQuery, setBookSearchQuery] = useState('');
  const [bookSearchResults, setBookSearchResults] = useState<BookLookupItem[]>([]);
  const [isSearchingBooks, setIsSearchingBooks] = useState(false);
  const [bookSearchError, setBookSearchError] = useState<string | null>(null);
  const [selectedBookId, setSelectedBookId] = useState<string | null>(null);
  const [listingFormState, setListingFormState] = useState<ListingFormState>(INITIAL_LISTING_FORM);
  const [bookFormState, setBookFormState] = useState<BookFormState>(INITIAL_BOOK_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState<string | null>(null);
  const [createdListingId, setCreatedListingId] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    void loadSellerProfile();
  }, [loadSellerProfile]);

  const selectedBook = useMemo(
    () => bookSearchResults.find((book) => book.id === selectedBookId) ?? null,
    [bookSearchResults, selectedBookId],
  );

  const hasActiveSellerProfile = Boolean(sellerProfile?.isActive);
  const isCheckingSellerProfile =
    (sellerProfileLoadState === 'loading' || sellerProfileLoadState === 'idle') && !sellerProfile;

  const validateForm = (): string | null => {
    const price = parsePositiveNumber(listingFormState.price);
    const quantity = parsePositiveInteger(listingFormState.quantity);
    const descriptionLength = listingFormState.description.trim().length;

    if (createMode === 'existingBook' && !selectedBookId) {
      return t('pages.myListingCreate.validation.bookRequired');
    }

    if (createMode === 'missingBook') {
      if (!bookFormState.title.trim()) {
        return t('pages.myListingCreate.validation.bookTitleRequired');
      }

      if (!bookFormState.author.trim()) {
        return t('pages.myListingCreate.validation.bookAuthorRequired');
      }

      if (!bookFormState.genre.trim()) {
        return t('pages.myListingCreate.validation.bookGenreRequired');
      }
    }

    if (price < 0.01) {
      return t('pages.myListingCreate.validation.priceRequired');
    }

    if (quantity < 1) {
      return t('pages.myListingCreate.validation.quantityRequired');
    }

    if (descriptionLength < 10) {
      return t('pages.myListingCreate.validation.descriptionMinLength');
    }

    return null;
  };

  const handleBookSearch = async (): Promise<void> => {
    setBookSearchError(null);
    setIsSearchingBooks(true);

    try {
      const results = await booksApi.lookupBooks(bookSearchQuery, 10);
      setBookSearchResults(results);

      if (selectedBookId && !results.some((book) => book.id === selectedBookId)) {
        setSelectedBookId(null);
      }
    } catch (error: unknown) {
      setBookSearchResults([]);
      setBookSearchError(getApiErrorMessage(error, t('pages.myListingCreate.bookSearchError')));
    } finally {
      setIsSearchingBooks(false);
    }
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setFormError(null);
    setFormSuccess(null);

    const validationError = validateForm();
    if (validationError) {
      setFormError(validationError);
      return;
    }

    setIsSubmitting(true);

    try {
      const listingId =
        createMode === 'missingBook'
          ? await listingsApi.createListingWithBook({
              title: bookFormState.title,
              author: bookFormState.author,
              genre: bookFormState.genre,
              ...(bookFormState.description.trim()
                ? { bookDescription: bookFormState.description }
                : {}),
              ...(bookFormState.publisher.trim() ? { publisher: bookFormState.publisher } : {}),
              ...(bookFormState.publishedOn ? { publishedOn: bookFormState.publishedOn } : {}),
              ...(bookFormState.isbn.trim() ? { isbn: bookFormState.isbn } : {}),
              price: parsePositiveNumber(listingFormState.price),
              currency: DEFAULT_CURRENCY,
              condition: listingFormState.condition,
              quantity: parsePositiveInteger(listingFormState.quantity),
              description: listingFormState.description,
              image: listingFormState.image,
            })
          : await listingsApi.createListing({
              bookId: selectedBookId!,
              price: parsePositiveNumber(listingFormState.price),
              currency: DEFAULT_CURRENCY,
              condition: listingFormState.condition,
              quantity: parsePositiveInteger(listingFormState.quantity),
              description: listingFormState.description,
              image: listingFormState.image,
            });

      setCreatedListingId(listingId);
      setFormSuccess(t('pages.myListingCreate.submitSuccess'));
    } catch (error: unknown) {
      setFormError(getApiErrorMessage(error, t('pages.myListingCreate.submitError')));
    } finally {
      setIsSubmitting(false);
    }
  };

  const resetAll = (): void => {
    setCreateMode('existingBook');
    setBookSearchQuery('');
    setBookSearchResults([]);
    setSelectedBookId(null);
    setListingFormState(INITIAL_LISTING_FORM);
    setBookFormState(INITIAL_BOOK_FORM);
    setFormError(null);
    setFormSuccess(null);
    setCreatedListingId(null);
  };

  if (isCheckingSellerProfile) {
    return (
      <Container className="account-page">
        <LoadingState
          description={t('pages.myListingCreate.loadingSellerDescription')}
          title={t('pages.myListingCreate.loadingSellerTitle')}
        />
      </Container>
    );
  }

  if (!hasActiveSellerProfile) {
    return (
      <Container className="account-page">
        <header className="marketplace-header">
          <h1>{t('pages.myListingCreate.title')}</h1>
          <p>{t('pages.myListingCreate.subtitle')}</p>
        </header>
        <SellerProfileRequiredState isInactive={Boolean(sellerProfile)} />
      </Container>
    );
  }

  if (createdListingId && formSuccess) {
    return (
      <Container className="account-page">
        <header className="marketplace-header">
          <h1>{t('pages.myListingCreate.title')}</h1>
          <p>{t('pages.myListingCreate.subtitle')}</p>
        </header>
        <Card className="seller-listing-success-card" elevated>
          <h2>{t('pages.myListingCreate.successTitle')}</h2>
          <p>{formSuccess}</p>
          <p>{t('pages.myListingCreate.moderationNotice')}</p>
          <div className="profile-actions">
            <Link to={ROUTES.myListings}>
              <Button>{t('pages.myListingCreate.goToMyListings')}</Button>
            </Link>
            <Button onClick={resetAll} variant="secondary">
              {t('pages.myListingCreate.createAnother')}
            </Button>
          </div>
        </Card>
      </Container>
    );
  }

  return (
    <Container className="account-page">
      <header className="marketplace-header">
        <h1>{t('pages.myListingCreate.title')}</h1>
        <p>{t('pages.myListingCreate.subtitle')}</p>
      </header>

      <Card className="seller-listing-form-card" elevated>
        <form className="seller-listing-form" onSubmit={handleSubmit}>
          <section className="seller-listing-section">
            <h2>{t('pages.myListingCreate.bookStepTitle')}</h2>
            <p>{t('pages.myListingCreate.bookStepDescription')}</p>

            <div className="seller-listing-mode-switch">
              <Button
                onClick={() => {
                  setCreateMode('existingBook');
                  setFormError(null);
                }}
                type="button"
                variant={createMode === 'existingBook' ? 'primary' : 'secondary'}
              >
                {t('pages.myListingCreate.existingBookMode')}
              </Button>
              <Button
                onClick={() => {
                  setCreateMode('missingBook');
                  setFormError(null);
                }}
                type="button"
                variant={createMode === 'missingBook' ? 'primary' : 'secondary'}
              >
                {t('pages.myListingCreate.missingBookMode')}
              </Button>
            </div>

            {createMode === 'existingBook' ? (
              <div className="seller-listing-book-search">
                <Input
                  label={t('pages.myListingCreate.bookSearchLabel')}
                  onChange={(event) => {
                    setBookSearchQuery(event.target.value);
                  }}
                  placeholder={t('pages.myListingCreate.bookSearchPlaceholder')}
                  value={bookSearchQuery}
                />
                <Button
                  disabled={isSearchingBooks}
                  onClick={() => {
                    void handleBookSearch();
                  }}
                  type="button"
                  variant="secondary"
                >
                  {isSearchingBooks
                    ? t('pages.myListingCreate.bookSearchLoading')
                    : t('pages.myListingCreate.bookSearchAction')}
                </Button>

                {bookSearchError ? <p className="auth-error">{bookSearchError}</p> : null}

                {bookSearchResults.length > 0 ? (
                  <div className="seller-book-results">
                    {bookSearchResults.map((book) => (
                      <button
                        className={`seller-book-result ${selectedBookId === book.id ? 'seller-book-result--selected' : ''}`}
                        key={book.id}
                        onClick={() => {
                          setSelectedBookId(book.id);
                        }}
                        type="button"
                      >
                        <strong>{book.title}</strong>
                        <span>
                          {book.author} • {book.genre}
                        </span>
                        {book.isbn ? <span>{book.isbn}</span> : null}
                      </button>
                    ))}
                  </div>
                ) : null}

                {bookSearchResults.length === 0 &&
                bookSearchQuery.trim() &&
                !isSearchingBooks &&
                !bookSearchError ? (
                  <p className="ui-input-hint">{t('pages.myListingCreate.noBookSearchResults')}</p>
                ) : null}

                {selectedBook ? (
                  <p className="ui-input-hint">
                    {t('pages.myListingCreate.selectedBookLabel')}: {selectedBook.title} (
                    {selectedBook.author})
                  </p>
                ) : null}
              </div>
            ) : (
              <div className="seller-listing-book-create-grid">
                <Input
                  label={t('pages.myListingCreate.bookTitleLabel')}
                  onChange={(event) => {
                    setBookFormState((previousState) => ({
                      ...previousState,
                      title: event.target.value,
                    }));
                  }}
                  value={bookFormState.title}
                />
                <Input
                  label={t('pages.myListingCreate.bookAuthorLabel')}
                  onChange={(event) => {
                    setBookFormState((previousState) => ({
                      ...previousState,
                      author: event.target.value,
                    }));
                  }}
                  value={bookFormState.author}
                />
                <Input
                  label={t('pages.myListingCreate.bookGenreLabel')}
                  onChange={(event) => {
                    setBookFormState((previousState) => ({
                      ...previousState,
                      genre: event.target.value,
                    }));
                  }}
                  value={bookFormState.genre}
                />
                <Input
                  label={t('pages.myListingCreate.bookPublisherLabel')}
                  onChange={(event) => {
                    setBookFormState((previousState) => ({
                      ...previousState,
                      publisher: event.target.value,
                    }));
                  }}
                  value={bookFormState.publisher}
                />
                <Input
                  label={t('pages.myListingCreate.bookIsbnLabel')}
                  onChange={(event) => {
                    setBookFormState((previousState) => ({
                      ...previousState,
                      isbn: event.target.value,
                    }));
                  }}
                  value={bookFormState.isbn}
                />
                <Input
                  label={t('pages.myListingCreate.bookPublishedOnLabel')}
                  onChange={(event) => {
                    setBookFormState((previousState) => ({
                      ...previousState,
                      publishedOn: event.target.value,
                    }));
                  }}
                  type="date"
                  value={bookFormState.publishedOn}
                />
                <label className="ui-input-wrapper">
                  <span className="ui-input-label">
                    {t('pages.myListingCreate.bookDescriptionLabel')}
                  </span>
                  <textarea
                    className="ui-input seller-listing-textarea"
                    onChange={(event) => {
                      setBookFormState((previousState) => ({
                        ...previousState,
                        description: event.target.value,
                      }));
                    }}
                    value={bookFormState.description}
                  />
                </label>
                <p className="ui-input-hint">{t('pages.myListingCreate.missingBookFlowHint')}</p>
              </div>
            )}
          </section>

          <section className="seller-listing-section">
            <h2>{t('pages.myListingCreate.listingStepTitle')}</h2>
            <p>{t('pages.myListingCreate.listingStepDescription')}</p>
            <p className="ui-input-hint">
              {t('pages.myListingCreate.currencyHint', { currency: DEFAULT_CURRENCY })}
            </p>

            <div className="seller-listing-book-create-grid">
              <Input
                label={t('pages.myListingCreate.priceLabel')}
                min={0.01}
                onChange={(event) => {
                  setListingFormState((previousState) => ({
                    ...previousState,
                    price: event.target.value,
                  }));
                }}
                step={0.01}
                type="number"
                value={listingFormState.price}
              />
              <Input
                label={t('pages.myListingCreate.quantityLabel')}
                min={1}
                onChange={(event) => {
                  setListingFormState((previousState) => ({
                    ...previousState,
                    quantity: event.target.value,
                  }));
                }}
                type="number"
                value={listingFormState.quantity}
              />
              <label className="ui-input-wrapper">
                <span className="ui-input-label">{t('pages.myListingCreate.conditionLabel')}</span>
                <select
                  className="ui-input"
                  onChange={(event) => {
                    setListingFormState((previousState) => ({
                      ...previousState,
                      condition: event.target.value as MarketplaceListingCondition,
                    }));
                  }}
                  value={listingFormState.condition}
                >
                  {CONDITION_OPTIONS.map((condition) => (
                    <option key={condition} value={condition}>
                      {t(`taxonomy.conditions.${condition}`)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="ui-input-wrapper seller-listing-grid-span-full">
                <span className="ui-input-label">
                  {t('pages.myListingCreate.descriptionLabel')}
                </span>
                <textarea
                  className="ui-input seller-listing-textarea"
                  onChange={(event) => {
                    setListingFormState((previousState) => ({
                      ...previousState,
                      description: event.target.value,
                    }));
                  }}
                  value={listingFormState.description}
                />
              </label>

              <label className="ui-input-wrapper seller-listing-grid-span-full">
                <span className="ui-input-label">{t('pages.myListingCreate.imageLabel')}</span>
                <input
                  className="ui-input"
                  onChange={(event) => {
                    const nextImage = event.target.files?.[0] ?? null;
                    setListingFormState((previousState) => ({
                      ...previousState,
                      image: nextImage,
                    }));
                  }}
                  type="file"
                />
                <span className="ui-input-hint">{t('pages.myListingCreate.imageHint')}</span>
              </label>
            </div>
          </section>

          {formError ? <p className="auth-error">{formError}</p> : null}

          <div className="profile-actions">
            <Button disabled={isSubmitting} type="submit">
              {isSubmitting
                ? t('pages.myListingCreate.submitting')
                : t('pages.myListingCreate.submit')}
            </Button>
            <Button onClick={resetAll} type="button" variant="secondary">
              {t('common.actions.reset')}
            </Button>
          </div>
        </form>
      </Card>
    </Container>
  );
}
