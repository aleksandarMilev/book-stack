import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import { MarketplaceFilters } from '@/features/marketplace/components/MarketplaceFilters';
import { MarketplaceListingCard } from '@/features/marketplace/components/MarketplaceListingCard';
import {
  MARKETPLACE_CONDITION_TO_BACKEND,
  MARKETPLACE_SORT_TO_BACKEND,
  type MarketplaceSortOption,
} from '@/features/marketplace/types';
import { useDisclosure } from '@/hooks/useDisclosure';
import { usePaginationScrollReset } from '@/hooks/usePaginationScrollReset';
import type { MarketplaceListing, MarketplaceListingCondition } from '@/types/marketplace.types';
import { classNames } from '@/utils/classNames';

const SORT_OPTIONS: MarketplaceSortOption[] = [
  'newest',
  'oldest',
  'priceAsc',
  'priceDesc',
  'titleAsc',
  'titleDesc',
];

const PAGE_SIZE_OPTIONS = [10, 20, 30] as const;

interface MarketplaceQueryState {
  search: string;
  title: string;
  author: string;
  genre: string;
  condition: MarketplaceListingCondition | 'all';
  priceFrom: string;
  priceTo: string;
  sort: MarketplaceSortOption;
  pageIndex: number;
  pageSize: number;
}

const DEFAULT_QUERY_STATE: MarketplaceQueryState = {
  search: '',
  title: '',
  author: '',
  genre: '',
  condition: 'all',
  priceFrom: '',
  priceTo: '',
  sort: 'newest',
  pageIndex: 1,
  pageSize: 10,
};

const parsePositiveInt = (value: string | null, fallback: number): number => {
  const parsedValue = Number.parseInt(value ?? '', 10);

  if (Number.isNaN(parsedValue) || parsedValue < 1) {
    return fallback;
  }

  return parsedValue;
};

const isSortOption = (value: string): value is MarketplaceSortOption =>
  SORT_OPTIONS.includes(value as MarketplaceSortOption);

const isConditionOption = (value: string): value is MarketplaceListingCondition =>
  ['new', 'likeNew', 'veryGood', 'good', 'acceptable', 'poor'].includes(value);

const normalizePageSize = (value: number): number =>
  PAGE_SIZE_OPTIONS.includes(value as (typeof PAGE_SIZE_OPTIONS)[number]) ? value : DEFAULT_QUERY_STATE.pageSize;

const parseQueryState = (searchParams: URLSearchParams): MarketplaceQueryState => {
  const sortParam = searchParams.get('sort');
  const conditionParam = searchParams.get('condition');

  return {
    search: searchParams.get('q') ?? DEFAULT_QUERY_STATE.search,
    title: searchParams.get('title') ?? DEFAULT_QUERY_STATE.title,
    author: searchParams.get('author') ?? DEFAULT_QUERY_STATE.author,
    genre: searchParams.get('genre') ?? DEFAULT_QUERY_STATE.genre,
    condition: conditionParam && isConditionOption(conditionParam) ? conditionParam : DEFAULT_QUERY_STATE.condition,
    priceFrom: searchParams.get('priceFrom') ?? DEFAULT_QUERY_STATE.priceFrom,
    priceTo: searchParams.get('priceTo') ?? DEFAULT_QUERY_STATE.priceTo,
    sort: sortParam && isSortOption(sortParam) ? sortParam : DEFAULT_QUERY_STATE.sort,
    pageIndex: parsePositiveInt(searchParams.get('page'), DEFAULT_QUERY_STATE.pageIndex),
    pageSize: normalizePageSize(parsePositiveInt(searchParams.get('pageSize'), DEFAULT_QUERY_STATE.pageSize)),
  };
};

const buildQueryParams = (state: MarketplaceQueryState): URLSearchParams => {
  const params = new URLSearchParams();

  if (state.search) params.set('q', state.search);
  if (state.title) params.set('title', state.title);
  if (state.author) params.set('author', state.author);
  if (state.genre) params.set('genre', state.genre);
  if (state.condition !== 'all') params.set('condition', state.condition);
  if (state.priceFrom) params.set('priceFrom', state.priceFrom);
  if (state.priceTo) params.set('priceTo', state.priceTo);
  if (state.sort !== DEFAULT_QUERY_STATE.sort) params.set('sort', state.sort);
  if (state.pageIndex !== DEFAULT_QUERY_STATE.pageIndex) params.set('page', String(state.pageIndex));
  if (state.pageSize !== DEFAULT_QUERY_STATE.pageSize) params.set('pageSize', String(state.pageSize));

  return params;
};

const parseOptionalNumber = (value: string): number | undefined => {
  if (!value) {
    return undefined;
  }

  const parsedValue = Number.parseFloat(value);
  if (Number.isNaN(parsedValue)) {
    return undefined;
  }

  return parsedValue;
};

export function MarketplacePage() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const mobileFilters = useDisclosure();
  const isMobileFiltersOpen = mobileFilters.isOpen;
  const closeMobileFilters = mobileFilters.close;

  const queryState = useMemo(() => parseQueryState(searchParams), [searchParams]);
  const resultsSectionRef = usePaginationScrollReset<HTMLDivElement>(queryState.pageIndex);

  const [listings, setListings] = useState<MarketplaceListing[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [reloadCounter, setReloadCounter] = useState(0);

  const totalPages = useMemo(
    () => Math.max(1, Math.ceil(totalItems / Math.max(queryState.pageSize, 1))),
    [queryState.pageSize, totalItems],
  );

  const updateQueryState = useCallback(
    (partialState: Partial<MarketplaceQueryState>, resetPageIndex = true) => {
      const nextState: MarketplaceQueryState = {
        ...queryState,
        ...partialState,
        ...(resetPageIndex ? { pageIndex: 1 } : {}),
      };

      setSearchParams(buildQueryParams(nextState), { replace: true });
    },
    [queryState, setSearchParams],
  );

  const handleRetryFetch = useCallback(() => {
    setReloadCounter((counter) => counter + 1);
  }, []);

  useEffect(() => {
    let isActive = true;

    const loadListings = async (): Promise<void> => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const response = await listingsApi.getListings({
          searchTerm: queryState.search || undefined,
          title: queryState.title || undefined,
          author: queryState.author || undefined,
          genre: queryState.genre || undefined,
          condition:
            queryState.condition === 'all' ? undefined : MARKETPLACE_CONDITION_TO_BACKEND[queryState.condition],
          priceFrom: parseOptionalNumber(queryState.priceFrom),
          priceTo: parseOptionalNumber(queryState.priceTo),
          sorting: MARKETPLACE_SORT_TO_BACKEND[queryState.sort],
          pageIndex: queryState.pageIndex,
          pageSize: queryState.pageSize,
          isApproved: true,
        });

        if (!isActive) {
          return;
        }

        setListings(response.items);
        setTotalItems(response.totalItems);
      } catch (error: unknown) {
        if (!isActive) {
          return;
        }

        setListings([]);
        setTotalItems(0);
        setErrorMessage(getApiErrorMessage(error, t('marketplace.errorDescription')));
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    void loadListings();

    return () => {
      isActive = false;
    };
  }, [queryState, reloadCounter, t]);

  useEffect(() => {
    if (queryState.pageIndex <= totalPages) {
      return;
    }

    updateQueryState({ pageIndex: totalPages }, false);
  }, [queryState.pageIndex, totalPages, updateQueryState]);

  useEffect(() => {
    if (!isMobileFiltersOpen) {
      return;
    }

    const handleEscape = (event: KeyboardEvent): void => {
      if (event.key === 'Escape') {
        closeMobileFilters();
      }
    };

    window.addEventListener('keydown', handleEscape);
    return () => {
      window.removeEventListener('keydown', handleEscape);
    };
  }, [closeMobileFilters, isMobileFiltersOpen]);

  const clearFilters = (): void => {
    setSearchParams(buildQueryParams(DEFAULT_QUERY_STATE), { replace: true });
  };

  const hasListings = !isLoading && !errorMessage && listings.length > 0;

  const goToPreviousPage = (): void => {
    if (queryState.pageIndex <= 1) {
      return;
    }

    updateQueryState({ pageIndex: queryState.pageIndex - 1 }, false);
  };

  const goToNextPage = (): void => {
    if (queryState.pageIndex >= totalPages) {
      return;
    }

    updateQueryState({ pageIndex: queryState.pageIndex + 1 }, false);
  };

  return (
    <Container className="marketplace-page">
      <header className="marketplace-header" data-reveal>
        <h1>{t('marketplace.title')}</h1>
        <p>{t('marketplace.subtitle')}</p>
      </header>

      <section className="marketplace-toolbar" data-reveal>
        <Input
          label={t('marketplace.searchLabel')}
          name="search"
          onChange={(event) => {
            updateQueryState({ search: event.target.value });
          }}
          placeholder={t('marketplace.searchPlaceholder')}
          value={queryState.search}
        />

        <label className="marketplace-sort-label" htmlFor="marketplace-sort">
          <span>{t('marketplace.sortLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="marketplace-sort"
            onChange={(event) => {
              if (isSortOption(event.target.value)) {
                updateQueryState({ sort: event.target.value });
              }
            }}
            value={queryState.sort}
          >
            {SORT_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {t(`taxonomy.sort.${option}`)}
              </option>
            ))}
          </select>
        </label>

        <label className="marketplace-sort-label" htmlFor="marketplace-page-size">
          <span>{t('marketplace.pageSizeLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="marketplace-page-size"
            onChange={(event) => {
              updateQueryState({ pageSize: normalizePageSize(Number.parseInt(event.target.value, 10)) });
            }}
            value={queryState.pageSize}
          >
            {PAGE_SIZE_OPTIONS.map((pageSize) => (
              <option key={pageSize} value={pageSize}>
                {t('marketplace.pageSizeOption', { count: pageSize })}
              </option>
            ))}
          </select>
        </label>

        <Button
          aria-controls="marketplace-mobile-filters"
          aria-expanded={mobileFilters.isOpen}
          aria-label={t('common.actions.openFilters')}
          className="marketplace-mobile-filters-trigger"
          onClick={mobileFilters.open}
          variant="secondary"
        >
          {t('common.actions.openFilters')}
        </Button>
      </section>

      <section className="marketplace-layout">
        <aside className="marketplace-desktop-filters" data-reveal>
          <p className="marketplace-filters-title">{t('marketplace.desktopFiltersTitle')}</p>
          <MarketplaceFilters
            author={queryState.author}
            genre={queryState.genre}
            onClearFilters={clearFilters}
            onAuthorChange={(value) => {
              updateQueryState({ author: value });
            }}
            onConditionChange={(value) => {
              updateQueryState({ condition: value });
            }}
            onGenreChange={(value) => {
              updateQueryState({ genre: value });
            }}
            onPriceFromChange={(value) => {
              updateQueryState({ priceFrom: value });
            }}
            onPriceToChange={(value) => {
              updateQueryState({ priceTo: value });
            }}
            onTitleChange={(value) => {
              updateQueryState({ title: value });
            }}
            priceFrom={queryState.priceFrom}
            priceTo={queryState.priceTo}
            selectedCondition={queryState.condition}
            title={queryState.title}
          />
        </aside>

        <div className="marketplace-results" data-reveal ref={resultsSectionRef}>
          <p className="marketplace-results-count">{t('marketplace.resultsCount', { count: totalItems })}</p>

          {isLoading ? (
            <LoadingState
              description={t('marketplace.loadingDescription')}
              title={t('marketplace.loadingTitle')}
            />
          ) : null}

          {!isLoading && errorMessage ? (
            <EmptyState
              action={<Button onClick={handleRetryFetch}>{t('common.actions.retry')}</Button>}
              description={errorMessage}
              title={t('marketplace.errorTitle')}
            />
          ) : null}

          {!isLoading && !errorMessage && listings.length === 0 ? (
            <EmptyState
              action={<Button onClick={clearFilters}>{t('common.actions.clearFilters')}</Button>}
              description={t('marketplace.emptyDescription')}
              title={t('marketplace.emptyTitle')}
            />
          ) : null}

          {hasListings ? (
            <div className="marketplace-grid">
              {listings.map((listing) => (
                <MarketplaceListingCard key={listing.id} listing={listing} />
              ))}
            </div>
          ) : null}

          {hasListings ? (
            <div className="marketplace-pagination">
              <Button disabled={queryState.pageIndex <= 1} onClick={goToPreviousPage} variant="secondary">
                {t('common.actions.previous')}
              </Button>
              <p>
                {t('marketplace.pageIndicator', { page: queryState.pageIndex, totalPages })}
              </p>
              <Button disabled={queryState.pageIndex >= totalPages} onClick={goToNextPage} variant="secondary">
                {t('common.actions.next')}
              </Button>
            </div>
          ) : null}
        </div>
      </section>

      <div
        className={classNames('marketplace-mobile-overlay', mobileFilters.isOpen && 'marketplace-mobile-overlay--open')}
        onClick={mobileFilters.close}
      />
      <aside
        aria-label={t('marketplace.mobileFiltersTitle')}
        aria-modal="true"
        aria-hidden={!mobileFilters.isOpen}
        className={classNames('marketplace-mobile-drawer', mobileFilters.isOpen && 'marketplace-mobile-drawer--open')}
        id="marketplace-mobile-filters"
        role="dialog"
      >
        <div className="marketplace-mobile-drawer-head">
          <p>{t('marketplace.mobileFiltersTitle')}</p>
          <Button onClick={mobileFilters.close} size="sm" variant="ghost">
            {t('common.actions.close')}
          </Button>
        </div>
        <MarketplaceFilters
          author={queryState.author}
          genre={queryState.genre}
          onClearFilters={clearFilters}
          onAuthorChange={(value) => {
            updateQueryState({ author: value });
          }}
          onConditionChange={(value) => {
            updateQueryState({ condition: value });
          }}
          onGenreChange={(value) => {
            updateQueryState({ genre: value });
          }}
          onPriceFromChange={(value) => {
            updateQueryState({ priceFrom: value });
          }}
          onPriceToChange={(value) => {
            updateQueryState({ priceTo: value });
          }}
          onTitleChange={(value) => {
            updateQueryState({ title: value });
          }}
          priceFrom={queryState.priceFrom}
          priceTo={queryState.priceTo}
          selectedCondition={queryState.condition}
          title={queryState.title}
        />
      </aside>
    </Container>
  );
}
