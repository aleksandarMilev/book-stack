import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { adminBooksModerationApi } from '@/features/admin/api/adminBooksModeration.api';
import { ModerationStatusBadge, RejectionReasonDialog } from '@/features/admin/components';
import type { AdminApprovalFilter, AdminBooksModerationItem,AdminBookSortOption } from '@/features/admin/types/admin.types';
import { deriveModerationStatus } from '@/features/admin/utils/moderationStatus';
import { useLanguage } from '@/hooks/useLanguage';
import { usePaginationScrollReset } from '@/hooks/usePaginationScrollReset';
import { formatDateTime } from '@/utils/formatters';

const SORT_OPTIONS: AdminBookSortOption[] = [
  'newest',
  'oldest',
  'titleAsc',
  'titleDesc',
  'publishedDateDesc',
  'publishedDateAsc',
];
const APPROVAL_FILTERS: AdminApprovalFilter[] = ['all', 'approved', 'unapproved'];
const PAGE_SIZE_OPTIONS = [10, 20, 30] as const;

export function AdminBooksModerationPage() {
  const { t } = useTranslation();
  const { language } = useLanguage();
  const [searchTerm, setSearchTerm] = useState('');
  const [approvalFilter, setApprovalFilter] = useState<AdminApprovalFilter>('all');
  const [sortOption, setSortOption] = useState<AdminBookSortOption>('newest');
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [books, setBooks] = useState<AdminBooksModerationItem[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [activeActionBookId, setActiveActionBookId] = useState<string | null>(null);
  const [rejectingBookId, setRejectingBookId] = useState<string | null>(null);
  const [isRejectSubmitting, setIsRejectSubmitting] = useState(false);
  const [reloadCounter, setReloadCounter] = useState(0);

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalItems / Math.max(pageSize, 1))), [pageSize, totalItems]);
  const resultsSectionRef = usePaginationScrollReset<HTMLDivElement>(pageIndex);

  const loadBooks = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await adminBooksModerationApi.getBooks({
        searchTerm: searchTerm || undefined,
        approval: approvalFilter,
        sorting: sortOption,
        pageIndex,
        pageSize,
      });

      setBooks(response.items);
      setTotalItems(response.totalItems);
    } catch (error: unknown) {
      setBooks([]);
      setTotalItems(0);
      setErrorMessage(getApiErrorMessage(error, t('pages.adminBooks.errorDescription')));
    } finally {
      setIsLoading(false);
    }
  }, [approvalFilter, pageIndex, pageSize, searchTerm, sortOption, t]);

  useEffect(() => {
    void loadBooks();
  }, [loadBooks, reloadCounter]);

  useEffect(() => {
    if (pageIndex <= totalPages) {
      return;
    }

    setPageIndex(totalPages);
  }, [pageIndex, totalPages]);

  const triggerReload = (): void => {
    setReloadCounter((previousCounter) => previousCounter + 1);
  };

  const handleApprove = async (bookId: string): Promise<void> => {
    setActionError(null);
    setActiveActionBookId(bookId);

    try {
      await adminBooksModerationApi.approveBook(bookId);
      triggerReload();
    } catch (error: unknown) {
      setActionError(getApiErrorMessage(error, t('pages.adminBooks.actionError')));
    } finally {
      setActiveActionBookId(null);
    }
  };

  const handleReject = async (reason: string): Promise<void> => {
    if (!rejectingBookId) {
      return;
    }

    setActionError(null);
    setIsRejectSubmitting(true);

    try {
      await adminBooksModerationApi.rejectBook(rejectingBookId, reason);
      setRejectingBookId(null);
      triggerReload();
    } catch (error: unknown) {
      setActionError(getApiErrorMessage(error, t('pages.adminBooks.actionError')));
    } finally {
      setIsRejectSubmitting(false);
    }
  };

  const handleDelete = async (bookId: string): Promise<void> => {
    const isConfirmed = window.confirm(t('pages.adminBooks.deleteConfirm'));
    if (!isConfirmed) {
      return;
    }

    setActionError(null);
    setActiveActionBookId(bookId);

    try {
      await adminBooksModerationApi.deleteBook(bookId);
      if (books.length === 1 && pageIndex > 1) {
        setPageIndex((previousPageIndex) => Math.max(previousPageIndex - 1, 1));
      } else {
        triggerReload();
      }
    } catch (error: unknown) {
      setActionError(getApiErrorMessage(error, t('pages.adminBooks.actionError')));
    } finally {
      setActiveActionBookId(null);
    }
  };

  const hasBooks = !isLoading && !errorMessage && books.length > 0;

  return (
    <Container className="admin-page">
      <header className="marketplace-header">
        <h1>{t('pages.adminBooks.title')}</h1>
        <p>{t('pages.adminBooks.subtitle')}</p>
      </header>

      <section className="marketplace-toolbar admin-toolbar">
        <Input
          label={t('pages.adminBooks.searchLabel')}
          onChange={(event) => {
            setPageIndex(1);
            setSearchTerm(event.target.value);
          }}
          placeholder={t('pages.adminBooks.searchPlaceholder')}
          value={searchTerm}
        />
        <label className="marketplace-sort-label" htmlFor="admin-books-approval-filter">
          <span>{t('pages.adminModeration.approvalFilterLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="admin-books-approval-filter"
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
        <label className="marketplace-sort-label" htmlFor="admin-books-sort">
          <span>{t('pages.adminModeration.sortLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="admin-books-sort"
            onChange={(event) => {
              setPageIndex(1);
              setSortOption(event.target.value as AdminBookSortOption);
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
        <label className="marketplace-sort-label" htmlFor="admin-books-page-size">
          <span>{t('marketplace.pageSizeLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="admin-books-page-size"
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
        <p className="marketplace-results-count">{t('pages.adminBooks.resultsCount', { count: totalItems })}</p>

        {actionError ? <p className="auth-error">{actionError}</p> : null}

        {isLoading ? (
          <LoadingState description={t('pages.adminBooks.loadingDescription')} title={t('pages.adminBooks.loadingTitle')} />
        ) : null}

        {!isLoading && errorMessage ? (
          <EmptyState
            action={<Button onClick={triggerReload}>{t('common.actions.retry')}</Button>}
            description={errorMessage}
            title={t('pages.adminBooks.errorTitle')}
          />
        ) : null}

        {!isLoading && !errorMessage && books.length === 0 ? (
          <EmptyState description={t('pages.adminBooks.emptyDescription')} title={t('pages.adminBooks.emptyTitle')} />
        ) : null}

        {hasBooks ? (
          <div className="admin-moderation-grid">
            {books.map((book) => {
              const status = deriveModerationStatus(book);
              const isPendingAction = activeActionBookId === book.id;

              return (
                <Card className="admin-moderation-card" key={book.id}>
                  <div className="admin-moderation-card-head">
                    <div>
                      <h2>{book.title}</h2>
                      <p className="admin-moderation-subtitle">{book.author}</p>
                    </div>
                    <ModerationStatusBadge status={status} />
                  </div>

                  <dl className="admin-moderation-metadata">
                    <dt>{t('pages.adminBooks.genreLabel')}</dt>
                    <dd>{book.genre}</dd>
                    {book.publisher ? (
                      <>
                        <dt>{t('pages.adminBooks.publisherLabel')}</dt>
                        <dd>{book.publisher}</dd>
                      </>
                    ) : null}
                    {book.isbn ? (
                      <>
                        <dt>{t('pages.adminBooks.isbnLabel')}</dt>
                        <dd>{book.isbn}</dd>
                      </>
                    ) : null}
                    <dt>{t('pages.adminModeration.createdOnLabel')}</dt>
                    <dd>{formatDateTime({ value: book.createdOn, language })}</dd>
                  </dl>

                  {status === 'rejected' && book.rejectionReason ? (
                    <p className="account-listing-rejection">
                      {t('pages.adminModeration.rejectionReasonLabel')}: {book.rejectionReason}
                    </p>
                  ) : null}

                  <div className="admin-moderation-actions">
                    <Button
                      disabled={isPendingAction}
                      onClick={() => {
                        void handleApprove(book.id);
                      }}
                      size="sm"
                      variant="secondary"
                    >
                      {t('common.actions.approve')}
                    </Button>
                    <Button
                      disabled={isPendingAction}
                      onClick={() => {
                        setRejectingBookId(book.id);
                      }}
                      size="sm"
                      variant="ghost"
                    >
                      {t('common.actions.reject')}
                    </Button>
                    <Button
                      disabled={isPendingAction}
                      onClick={() => {
                        void handleDelete(book.id);
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

        {hasBooks ? (
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
        isOpen={Boolean(rejectingBookId)}
        isSubmitting={isRejectSubmitting}
        onClose={() => {
          if (!isRejectSubmitting) {
            setRejectingBookId(null);
          }
        }}
        onSubmit={handleReject}
      />
    </Container>
  );
}
