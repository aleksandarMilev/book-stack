import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { Button, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import {
  MarketplaceFilters,
} from '@/features/marketplace/components/MarketplaceFilters';
import {
  MarketplaceListingCard,
} from '@/features/marketplace/components/MarketplaceListingCard';
import { marketplaceMockListings } from '@/features/marketplace/mockListings';
import type { SortOption } from '@/features/marketplace/types';
import { useDisclosure } from '@/hooks/useDisclosure';
import type { ListingCondition, MarketplaceGenre } from '@/types/marketplace.types';
import { classNames } from '@/utils/classNames';

const SORT_OPTIONS: SortOption[] = ['featured', 'priceAsc', 'priceDesc', 'newest'];

const isSortOption = (value: string): value is SortOption =>
  SORT_OPTIONS.includes(value as SortOption);

export function MarketplacePage() {
  const { t } = useTranslation();
  const mobileFilters = useDisclosure();

  const [searchQuery, setSearchQuery] = useState('');
  const [selectedGenres, setSelectedGenres] = useState<MarketplaceGenre[]>([]);
  const [selectedCondition, setSelectedCondition] = useState<ListingCondition | 'all'>('all');
  const [sortOption, setSortOption] = useState<SortOption>('featured');
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setIsLoading(false);
    }, 700);

    return () => {
      window.clearTimeout(timeout);
    };
  }, []);

  const filteredListings = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();

    const filtered = marketplaceMockListings.filter((listing) => {
      const title = t(listing.titleKey).toLowerCase();
      const author = t(listing.authorKey).toLowerCase();
      const genreMatches = selectedGenres.length === 0 || selectedGenres.includes(listing.genre);
      const conditionMatches = selectedCondition === 'all' || selectedCondition === listing.condition;
      const queryMatches = !normalizedQuery || title.includes(normalizedQuery) || author.includes(normalizedQuery);

      return genreMatches && conditionMatches && queryMatches;
    });

    if (sortOption === 'priceAsc') {
      return [...filtered].sort(
        (firstListing, secondListing) => firstListing.price.primary.amount - secondListing.price.primary.amount,
      );
    }

    if (sortOption === 'priceDesc') {
      return [...filtered].sort(
        (firstListing, secondListing) => secondListing.price.primary.amount - firstListing.price.primary.amount,
      );
    }

    if (sortOption === 'newest') {
      return [...filtered].reverse();
    }

    return filtered;
  }, [searchQuery, selectedCondition, selectedGenres, sortOption, t]);

  const handleToggleGenre = (genre: MarketplaceGenre): void => {
    setSelectedGenres((previousGenres) =>
      previousGenres.includes(genre)
        ? previousGenres.filter((existingGenre) => existingGenre !== genre)
        : [...previousGenres, genre],
    );
  };

  const clearFilters = (): void => {
    setSearchQuery('');
    setSelectedGenres([]);
    setSelectedCondition('all');
    setSortOption('featured');
  };

  return (
    <Container className="marketplace-page">
      <header className="marketplace-header">
        <h1>{t('marketplace.title')}</h1>
        <p>{t('marketplace.subtitle')}</p>
      </header>

      <section className="marketplace-toolbar">
        <Input
          label={t('marketplace.searchLabel')}
          name="search"
          onChange={(event) => {
            setSearchQuery(event.target.value);
          }}
          placeholder={t('marketplace.searchPlaceholder')}
          value={searchQuery}
        />

        <label className="marketplace-sort-label" htmlFor="marketplace-sort">
          <span>{t('marketplace.sortLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="marketplace-sort"
            onChange={(event) => {
              if (isSortOption(event.target.value)) {
                setSortOption(event.target.value);
              }
            }}
            value={sortOption}
          >
            {SORT_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {t(`taxonomy.sort.${option}`)}
              </option>
            ))}
          </select>
        </label>

        <Button className="marketplace-mobile-filters-trigger" onClick={mobileFilters.open} variant="secondary">
          {t('common.actions.openFilters')}
        </Button>
      </section>

      <section className="marketplace-layout">
        <aside className="marketplace-desktop-filters">
          <p className="marketplace-filters-title">{t('marketplace.desktopFiltersTitle')}</p>
          <MarketplaceFilters
            onClearFilters={clearFilters}
            onConditionChange={setSelectedCondition}
            onToggleGenre={handleToggleGenre}
            selectedCondition={selectedCondition}
            selectedGenres={selectedGenres}
          />
        </aside>

        <div className="marketplace-results">
          <p className="marketplace-results-count">{t('marketplace.resultsCount', { count: filteredListings.length })}</p>

          {isLoading ? (
            <LoadingState
              description={t('marketplace.loadingDescription')}
              title={t('marketplace.loadingTitle')}
            />
          ) : null}

          {!isLoading && filteredListings.length === 0 ? (
            <EmptyState
              action={<Button onClick={clearFilters}>{t('common.actions.clearFilters')}</Button>}
              description={t('marketplace.emptyDescription')}
              title={t('marketplace.emptyTitle')}
            />
          ) : null}

          {!isLoading && filteredListings.length > 0 ? (
            <div className="marketplace-grid">
              {filteredListings.map((listing) => (
                <MarketplaceListingCard key={listing.id} listing={listing} />
              ))}
            </div>
          ) : null}
        </div>
      </section>

      <div
        className={classNames('marketplace-mobile-overlay', mobileFilters.isOpen && 'marketplace-mobile-overlay--open')}
        onClick={mobileFilters.close}
      />
      <aside
        className={classNames('marketplace-mobile-drawer', mobileFilters.isOpen && 'marketplace-mobile-drawer--open')}
      >
        <div className="marketplace-mobile-drawer-head">
          <p>{t('marketplace.mobileFiltersTitle')}</p>
          <Button onClick={mobileFilters.close} size="sm" variant="ghost">
            {t('common.actions.close')}
          </Button>
        </div>
        <MarketplaceFilters
          onClearFilters={clearFilters}
          onConditionChange={setSelectedCondition}
          onToggleGenre={handleToggleGenre}
          selectedCondition={selectedCondition}
          selectedGenres={selectedGenres}
        />
      </aside>
    </Container>
  );
}
