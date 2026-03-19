import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import { Badge, Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import {
  MARKETPLACE_SORT_TO_BACKEND,
  type MarketplaceSortOption,
} from '@/features/marketplace/types';
import { SellerProfileRequiredState } from '@/features/sellerProfiles/components/SellerProfileRequiredState';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { usePaginationScrollReset } from '@/hooks/usePaginationScrollReset';
import { getListingDetailsRoute, getMyListingEditRoute, ROUTES } from '@/routes/paths';
import type { MarketplaceListing } from '@/types/marketplace.types';

const SORT_OPTIONS: MarketplaceSortOption[] = [
  'newest',
  'oldest',
  'priceAsc',
  'priceDesc',
  'titleAsc',
  'titleDesc',
];

const PAGE_SIZE_OPTIONS = [10, 20, 30] as const;

type ListingApprovalStatus = 'approved' | 'pending' | 'rejected';

const getApprovalStatus = (listing: MarketplaceListing): ListingApprovalStatus => {
  if (listing.isApproved) {
    return 'approved';
  }

  return listing.rejectionReason ? 'rejected' : 'pending';
};

const getStatusBadgeVariant = (status: ListingApprovalStatus): 'success' | 'warning' | 'danger' => {
  if (status === 'approved') {
    return 'success';
  }

  if (status === 'pending') {
    return 'warning';
  }

  return 'danger';
};

export function MyListingsPage() {
  const { t } = useTranslation();
  const sellerProfile = useSellerProfileStore((state) => state.profile);
  const sellerProfileLoadState = useSellerProfileStore((state) => state.loadState);
  const loadSellerProfile = useSellerProfileStore((state) => state.loadMine);
  const [searchTerm, setSearchTerm] = useState('');
  const [sortOption, setSortOption] = useState<MarketplaceSortOption>('newest');
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [listings, setListings] = useState<MarketplaceListing[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [deletingListingId, setDeletingListingId] = useState<string | null>(null);
  const [reloadCounter, setReloadCounter] = useState(0);

  const totalPages = useMemo(
    () => Math.max(1, Math.ceil(totalItems / Math.max(pageSize, 1))),
    [pageSize, totalItems],
  );
  const resultsSectionRef = usePaginationScrollReset<HTMLDivElement>(pageIndex);
  const hasActiveSellerProfile = Boolean(sellerProfile?.isActive);
  const isCheckingSellerProfile =
    (sellerProfileLoadState === 'loading' || sellerProfileLoadState === 'idle') && !sellerProfile;
  const hasOnlyPendingListings =
    listings.length > 0 && listings.every((listing) => getApprovalStatus(listing) === 'pending');

  const handleReload = useCallback(() => {
    setReloadCounter((previousCounter) => previousCounter + 1);
  }, []);

  useEffect(() => {
    void loadSellerProfile();
  }, [loadSellerProfile]);

  useEffect(() => {
    if (!hasActiveSellerProfile) {
      setIsLoading(false);
      setListings([]);
      setTotalItems(0);
      setErrorMessage(null);
      return;
    }

    let isActive = true;

    const loadMineListings = async (): Promise<void> => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const response = await listingsApi.getMineListings({
          searchTerm: searchTerm || undefined,
          sorting: MARKETPLACE_SORT_TO_BACKEND[sortOption],
          pageIndex,
          pageSize,
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
        setErrorMessage(getApiErrorMessage(error, t('pages.myListings.errorDescription')));
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    void loadMineListings();

    return () => {
      isActive = false;
    };
  }, [hasActiveSellerProfile, pageIndex, pageSize, reloadCounter, searchTerm, sortOption, t]);

  useEffect(() => {
    if (pageIndex <= totalPages) {
      return;
    }

    setPageIndex(totalPages);
  }, [pageIndex, totalPages]);

  const handleDeleteListing = async (listingId: string): Promise<void> => {
    const isConfirmed = window.confirm(t('pages.myListings.deleteConfirm'));
    if (!isConfirmed) {
      return;
    }

    setDeletingListingId(listingId);

    try {
      await listingsApi.deleteListing(listingId);
      if (listings.length === 1 && pageIndex > 1) {
        setPageIndex((previousPageIndex) => Math.max(previousPageIndex - 1, 1));
      } else {
        handleReload();
      }
    } catch (error: unknown) {
      setErrorMessage(getApiErrorMessage(error, t('pages.myListings.deleteError')));
    } finally {
      setDeletingListingId(null);
    }
  };

  const hasListings = !isLoading && !errorMessage && listings.length > 0;

  if (isCheckingSellerProfile) {
    return (
      <Container className="account-page">
        <LoadingState
          description={t('pages.myListings.loadingSellerDescription')}
          title={t('pages.myListings.loadingSellerTitle')}
        />
      </Container>
    );
  }

  if (!hasActiveSellerProfile) {
    return (
      <Container className="account-page">
        <header className="account-page-header">
          <div className="marketplace-header">
            <h1>{t('pages.myListings.title')}</h1>
            <p>{t('pages.myListings.subtitle')}</p>
          </div>
        </header>
        <SellerProfileRequiredState isInactive={Boolean(sellerProfile)} />
      </Container>
    );
  }

  return (
    <Container className="account-page">
      <header className="account-page-header">
        <div className="marketplace-header">
          <h1>{t('pages.myListings.title')}</h1>
          <p>{t('pages.myListings.subtitle')}</p>
        </div>
        <Link to={ROUTES.myListingCreate}>
          <Button>{t('pages.myListings.createCta')}</Button>
        </Link>
      </header>

      <section className="marketplace-toolbar account-toolbar">
        <Input
          label={t('pages.myListings.searchLabel')}
          onChange={(event) => {
            setPageIndex(1);
            setSearchTerm(event.target.value);
          }}
          placeholder={t('pages.myListings.searchPlaceholder')}
          value={searchTerm}
        />
        <label className="marketplace-sort-label" htmlFor="my-listings-sort">
          <span>{t('pages.myListings.sortLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="my-listings-sort"
            onChange={(event) => {
              const nextSort = event.target.value as MarketplaceSortOption;
              if (SORT_OPTIONS.includes(nextSort)) {
                setPageIndex(1);
                setSortOption(nextSort);
              }
            }}
            value={sortOption}
          >
            {SORT_OPTIONS.map((sort) => (
              <option key={sort} value={sort}>
                {t(`taxonomy.sort.${sort}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="marketplace-sort-label" htmlFor="my-listings-page-size">
          <span>{t('marketplace.pageSizeLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="my-listings-page-size"
            onChange={(event) => {
              setPageIndex(1);
              setPageSize(Number.parseInt(event.target.value, 10));
            }}
            value={pageSize}
          >
            {PAGE_SIZE_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {t('marketplace.pageSizeOption', { count: option })}
              </option>
            ))}
          </select>
        </label>
      </section>

      <div className="marketplace-results" ref={resultsSectionRef}>
        <p className="marketplace-results-count">
          {t('pages.myListings.resultsCount', { count: totalItems })}
        </p>

        {hasOnlyPendingListings ? (
          <Card className="seller-listings-info-card">
            <p>{t('pages.myListings.pendingModerationHint')}</p>
          </Card>
        ) : null}

        {isLoading ? (
          <LoadingState
            description={t('pages.myListings.loadingDescription')}
            title={t('pages.myListings.loadingTitle')}
          />
        ) : null}

        {!isLoading && errorMessage ? (
          <EmptyState
            action={<Button onClick={handleReload}>{t('common.actions.retry')}</Button>}
            description={errorMessage}
            title={t('pages.myListings.errorTitle')}
          />
        ) : null}

        {!isLoading && !errorMessage && listings.length === 0 ? (
          <EmptyState
            action={
              <Link to={ROUTES.myListingCreate}>
                <Button>{t('pages.myListings.createCta')}</Button>
              </Link>
            }
            description={t('pages.myListings.emptyDescription')}
            title={t('pages.myListings.emptyTitle')}
          />
        ) : null}

        {hasListings ? (
          <div className="account-listing-grid">
            {listings.map((listing) => {
              const approvalStatus = getApprovalStatus(listing);

              return (
                <Card className="account-listing-card" key={listing.id}>
                  {listing.imageUrl ? (
                    <img
                      alt={t('marketplace.listingImageAlt', { title: listing.title })}
                      className="marketplace-listing-image"
                      src={listing.imageUrl}
                    />
                  ) : (
                    <div className="marketplace-listing-image-placeholder" />
                  )}
                  <div className="account-listing-head">
                    <Badge variant={getStatusBadgeVariant(approvalStatus)}>
                      {t(`pages.myListings.status.${approvalStatus}`)}
                    </Badge>
                    <PriceDisplay value={listing.price} />
                  </div>
                  <h3>{listing.title}</h3>
                  <p className="marketplace-listing-author">{listing.author}</p>
                  <p className="marketplace-listing-meta">{listing.genre}</p>

                  {approvalStatus === 'rejected' && listing.rejectionReason ? (
                    <p className="account-listing-rejection">
                      {t('pages.myListings.rejectionReasonLabel')}: {listing.rejectionReason}
                    </p>
                  ) : null}

                  <div className="account-listing-actions">
                    <Link to={getListingDetailsRoute(listing.id)}>
                      <Button size="sm" variant="secondary">
                        {t('common.actions.viewListing')}
                      </Button>
                    </Link>
                    <Link to={getMyListingEditRoute(listing.id)}>
                      <Button size="sm" variant="ghost">
                        {t('pages.myListings.editAction')}
                      </Button>
                    </Link>
                    <Button
                      disabled={deletingListingId === listing.id}
                      onClick={() => {
                        void handleDeleteListing(listing.id);
                      }}
                      size="sm"
                      variant="ghost"
                    >
                      {deletingListingId === listing.id
                        ? t('pages.myListings.deletingAction')
                        : t('pages.myListings.deleteAction')}
                    </Button>
                  </div>
                </Card>
              );
            })}
          </div>
        ) : null}

        {hasListings ? (
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
