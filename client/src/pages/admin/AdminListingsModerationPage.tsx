import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import { Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { adminListingsModerationApi } from '@/features/admin/api/adminListingsModeration.api';
import { ModerationStatusBadge, RejectionReasonDialog } from '@/features/admin/components';
import type {
  AdminApprovalFilter,
  AdminListingsModerationQuery,
  AdminListingSortOption,
} from '@/features/admin/types/admin.types';
import { deriveModerationStatus } from '@/features/admin/utils/moderationStatus';
import { MARKETPLACE_CONDITIONS } from '@/features/marketplace/types';
import { useLanguage } from '@/hooks/useLanguage';
import { usePaginationScrollReset } from '@/hooks/usePaginationScrollReset';
import type { MarketplaceListing, MarketplaceListingCondition } from '@/types/marketplace.types';
import { formatDateTime } from '@/utils/formatters';

const SORT_OPTIONS: AdminListingSortOption[] = [
  'newest',
  'oldest',
  'priceAsc',
  'priceDesc',
  'titleAsc',
  'titleDesc',
  'publishedDateDesc',
  'publishedDateAsc',
];
const APPROVAL_FILTERS: AdminApprovalFilter[] = ['all', 'approved', 'unapproved'];
const PAGE_SIZE_OPTIONS = [10, 20, 30] as const;

export function AdminListingsModerationPage() {
  const { t } = useTranslation();
  const { language } = useLanguage();
  const [searchTerm, setSearchTerm] = useState('');
  const [approvalFilter, setApprovalFilter] = useState<AdminApprovalFilter>('all');
  const [conditionFilter, setConditionFilter] = useState<MarketplaceListingCondition | 'all'>('all');
  const [sortOption, setSortOption] = useState<AdminListingSortOption>('newest');
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [listings, setListings] = useState<MarketplaceListing[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [activeActionListingId, setActiveActionListingId] = useState<string | null>(null);
  const [rejectingListingId, setRejectingListingId] = useState<string | null>(null);
  const [isRejectSubmitting, setIsRejectSubmitting] = useState(false);
  const [reloadCounter, setReloadCounter] = useState(0);

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalItems / Math.max(pageSize, 1))), [pageSize, totalItems]);
  const resultsSectionRef = usePaginationScrollReset<HTMLDivElement>(pageIndex);

  const loadListings = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    const query: AdminListingsModerationQuery = {
      searchTerm: searchTerm || undefined,
      condition: conditionFilter === 'all' ? undefined : conditionFilter,
      approval: approvalFilter,
      sorting: sortOption,
      pageIndex,
      pageSize,
    };

    try {
      const response = await adminListingsModerationApi.getListings(query);
      setListings(response.items);
      setTotalItems(response.totalItems);
    } catch (error: unknown) {
      setListings([]);
      setTotalItems(0);
      setErrorMessage(getApiErrorMessage(error, t('pages.adminListings.errorDescription')));
    } finally {
      setIsLoading(false);
    }
  }, [approvalFilter, conditionFilter, pageIndex, pageSize, searchTerm, sortOption, t]);

  useEffect(() => {
    void loadListings();
  }, [loadListings, reloadCounter]);

  useEffect(() => {
    if (pageIndex <= totalPages) {
      return;
    }

    setPageIndex(totalPages);
  }, [pageIndex, totalPages]);

  const triggerReload = (): void => {
    setReloadCounter((previousCounter) => previousCounter + 1);
  };

  const handleApprove = async (listingId: string): Promise<void> => {
    setActionError(null);
    setActiveActionListingId(listingId);

    try {
      await adminListingsModerationApi.approveListing(listingId);
      triggerReload();
    } catch (error: unknown) {
      setActionError(getApiErrorMessage(error, t('pages.adminListings.actionError')));
    } finally {
      setActiveActionListingId(null);
    }
  };

  const handleReject = async (reason: string): Promise<void> => {
    if (!rejectingListingId) {
      return;
    }

    setActionError(null);
    setIsRejectSubmitting(true);

    try {
      await adminListingsModerationApi.rejectListing(rejectingListingId, reason);
      setRejectingListingId(null);
      triggerReload();
    } catch (error: unknown) {
      setActionError(getApiErrorMessage(error, t('pages.adminListings.actionError')));
    } finally {
      setIsRejectSubmitting(false);
    }
  };

  const handleDelete = async (listingId: string): Promise<void> => {
    const isConfirmed = window.confirm(t('pages.adminListings.deleteConfirm'));
    if (!isConfirmed) {
      return;
    }

    setActionError(null);
    setActiveActionListingId(listingId);

    try {
      await adminListingsModerationApi.deleteListing(listingId);
      if (listings.length === 1 && pageIndex > 1) {
        setPageIndex((previousPageIndex) => Math.max(previousPageIndex - 1, 1));
      } else {
        triggerReload();
      }
    } catch (error: unknown) {
      setActionError(getApiErrorMessage(error, t('pages.adminListings.actionError')));
    } finally {
      setActiveActionListingId(null);
    }
  };

  const hasListings = !isLoading && !errorMessage && listings.length > 0;

  return (
    <Container className="admin-page">
      <header className="marketplace-header">
        <h1>{t('pages.adminListings.title')}</h1>
        <p>{t('pages.adminListings.subtitle')}</p>
      </header>

      <section className="marketplace-toolbar admin-toolbar admin-toolbar--listings">
        <Input
          label={t('pages.adminListings.searchLabel')}
          onChange={(event) => {
            setPageIndex(1);
            setSearchTerm(event.target.value);
          }}
          placeholder={t('pages.adminListings.searchPlaceholder')}
          value={searchTerm}
        />
        <label className="marketplace-sort-label" htmlFor="admin-listings-approval-filter">
          <span>{t('pages.adminModeration.approvalFilterLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="admin-listings-approval-filter"
            onChange={(event) => {
              setPageIndex(1);
              setApprovalFilter(event.target.value as AdminApprovalFilter);
            }}
            value={approvalFilter}
          >
            {APPROVAL_FILTERS.map((filterValue) => (
              <option key={filterValue} value={filterValue}>
                {t(`pages.adminModeration.approvalFilter.${filterValue}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="marketplace-sort-label" htmlFor="admin-listings-condition-filter">
          <span>{t('marketplace.conditionLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="admin-listings-condition-filter"
            onChange={(event) => {
              setPageIndex(1);
              setConditionFilter(event.target.value as MarketplaceListingCondition | 'all');
            }}
            value={conditionFilter}
          >
            <option value="all">{t('taxonomy.conditions.all')}</option>
            {MARKETPLACE_CONDITIONS.map((condition) => (
              <option key={condition} value={condition}>
                {t(`taxonomy.conditions.${condition}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="marketplace-sort-label" htmlFor="admin-listings-sort">
          <span>{t('pages.adminModeration.sortLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="admin-listings-sort"
            onChange={(event) => {
              setPageIndex(1);
              setSortOption(event.target.value as AdminListingSortOption);
            }}
            value={sortOption}
          >
            {SORT_OPTIONS.map((sort) => (
              <option key={sort} value={sort}>
                {t(`pages.adminModeration.sort.${sort}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="marketplace-sort-label" htmlFor="admin-listings-page-size">
          <span>{t('marketplace.pageSizeLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="admin-listings-page-size"
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
        <p className="marketplace-results-count">{t('pages.adminListings.resultsCount', { count: totalItems })}</p>

        {actionError ? <p className="auth-error">{actionError}</p> : null}

        {isLoading ? (
          <LoadingState
            description={t('pages.adminListings.loadingDescription')}
            title={t('pages.adminListings.loadingTitle')}
          />
        ) : null}

        {!isLoading && errorMessage ? (
          <EmptyState
            action={<Button onClick={triggerReload}>{t('common.actions.retry')}</Button>}
            description={errorMessage}
            title={t('pages.adminListings.errorTitle')}
          />
        ) : null}

        {!isLoading && !errorMessage && listings.length === 0 ? (
          <EmptyState description={t('pages.adminListings.emptyDescription')} title={t('pages.adminListings.emptyTitle')} />
        ) : null}

        {hasListings ? (
          <div className="admin-moderation-grid">
            {listings.map((listing) => {
              const status = deriveModerationStatus(listing);
              const isPendingAction = activeActionListingId === listing.id;

              return (
                <Card className="admin-moderation-card" key={listing.id}>
                  {listing.imageUrl ? (
                    <img
                      alt={t('marketplace.listingImageAlt', { title: listing.title })}
                      className="marketplace-listing-image"
                      src={listing.imageUrl}
                    />
                  ) : (
                    <div className="marketplace-listing-image-placeholder" />
                  )}

                  <div className="admin-moderation-card-head">
                    <div>
                      <h2>{listing.title}</h2>
                      <p className="admin-moderation-subtitle">{listing.author}</p>
                    </div>
                    <div className="admin-moderation-card-head-right">
                      <ModerationStatusBadge status={status} />
                      <PriceDisplay value={listing.price} />
                    </div>
                  </div>

                  <dl className="admin-moderation-metadata">
                    <dt>{t('pages.adminListings.genreLabel')}</dt>
                    <dd>{listing.genre}</dd>
                    <dt>{t('pages.adminListings.conditionLabel')}</dt>
                    <dd>{t(`taxonomy.conditions.${listing.condition}`)}</dd>
                    <dt>{t('pages.adminListings.sellerLabel')}</dt>
                    <dd>{listing.creatorId}</dd>
                    <dt>{t('pages.adminModeration.createdOnLabel')}</dt>
                    <dd>{formatDateTime({ value: listing.createdOn, language })}</dd>
                  </dl>

                  {status === 'rejected' && listing.rejectionReason ? (
                    <p className="account-listing-rejection">
                      {t('pages.adminModeration.rejectionReasonLabel')}: {listing.rejectionReason}
                    </p>
                  ) : null}

                  <div className="admin-moderation-actions">
                    <Button
                      disabled={isPendingAction}
                      onClick={() => {
                        void handleApprove(listing.id);
                      }}
                      size="sm"
                      variant="secondary"
                    >
                      {t('common.actions.approve')}
                    </Button>
                    <Button
                      disabled={isPendingAction}
                      onClick={() => {
                        setRejectingListingId(listing.id);
                      }}
                      size="sm"
                      variant="ghost"
                    >
                      {t('common.actions.reject')}
                    </Button>
                    <Button
                      disabled={isPendingAction}
                      onClick={() => {
                        void handleDelete(listing.id);
                      }}
                      size="sm"
                      variant="ghost"
                    >
                      {isPendingAction ? t('pages.adminModeration.deletingAction') : t('common.actions.delete')}
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

      <RejectionReasonDialog
        isOpen={Boolean(rejectingListingId)}
        isSubmitting={isRejectSubmitting}
        onClose={() => {
          if (!isRejectSubmitting) {
            setRejectingListingId(null);
          }
        }}
        onSubmit={handleReject}
      />
    </Container>
  );
}
