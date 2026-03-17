import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Badge, Button, Card, Container, EmptyState, LoadingState } from '@/components/ui';
import { adminStatisticsApi } from '@/features/admin/api/adminStatistics.api';
import { AdminMetricCard } from '@/features/admin/components';
import type { AdminMonthlyRevenue, AdminStatistics } from '@/features/admin/types/admin.types';
import { useLanguage } from '@/hooks/useLanguage';
import type { CurrencyCode } from '@/types/pricing.types';
import { formatMoney, resolveLocale } from '@/utils/formatters';

const toCurrencyCode = (currency: string): CurrencyCode | undefined => {
  const normalizedCurrency = currency.trim().toUpperCase();
  if (normalizedCurrency === 'BGN' || normalizedCurrency === 'EUR') {
    return normalizedCurrency;
  }

  return undefined;
};

const formatRevenueValue = (revenue: AdminMonthlyRevenue, language: string): string => {
  const currency = toCurrencyCode(revenue.currency);
  if (currency) {
    return formatMoney({ amount: revenue.revenue, currency, language });
  }

  return `${new Intl.NumberFormat(resolveLocale(language)).format(revenue.revenue)} ${revenue.currency}`;
};

const getMonthLabel = (revenue: AdminMonthlyRevenue, language: string): string => {
  const date = new Date(Date.UTC(revenue.year, revenue.month - 1, 1));

  return new Intl.DateTimeFormat(resolveLocale(language), {
    month: 'long',
    year: 'numeric',
  }).format(date);
};

export function AdminDashboardPage() {
  const { t } = useTranslation();
  const { language } = useLanguage();
  const [stats, setStats] = useState<AdminStatistics | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const loadStatistics = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await adminStatisticsApi.getStatistics();
      setStats(response);
    } catch (error: unknown) {
      setStats(null);
      setErrorMessage(getApiErrorMessage(error, t('pages.adminDashboard.errorDescription')));
    } finally {
      setIsLoading(false);
    }
  }, [t]);

  useEffect(() => {
    void loadStatistics();
  }, [loadStatistics]);

  return (
    <Container className="admin-page">
      <header className="marketplace-header">
        <h1>{t('pages.adminDashboard.title')}</h1>
        <p>{t('pages.adminDashboard.subtitle')}</p>
      </header>

      {isLoading ? (
        <LoadingState
          description={t('pages.adminDashboard.loadingDescription')}
          title={t('pages.adminDashboard.loadingTitle')}
        />
      ) : null}

      {!isLoading && errorMessage ? (
        <EmptyState
          action={<Button onClick={() => void loadStatistics()}>{t('common.actions.retry')}</Button>}
          description={errorMessage}
          title={t('pages.adminDashboard.errorTitle')}
        />
      ) : null}

      {!isLoading && !errorMessage && stats ? (
        <>
          <section className="admin-stats-grid">
            <AdminMetricCard label={t('pages.adminDashboard.metrics.totalUsers')} value={stats.totalUsers} />
            <AdminMetricCard label={t('pages.adminDashboard.metrics.totalBooks')} value={stats.totalBooks} />
            <AdminMetricCard label={t('pages.adminDashboard.metrics.totalListings')} value={stats.totalListings} />
            <AdminMetricCard label={t('pages.adminDashboard.metrics.pendingBooks')} value={stats.pendingBooks} />
            <AdminMetricCard label={t('pages.adminDashboard.metrics.pendingListings')} value={stats.pendingListings} />
            <AdminMetricCard label={t('pages.adminDashboard.metrics.totalOrders')} value={stats.totalOrders} />
            <AdminMetricCard label={t('pages.adminDashboard.metrics.paidOrders')} value={stats.paidOrders} />
          </section>

          <Card className="admin-revenue-card">
            <div className="admin-revenue-header">
              <h2>{t('pages.adminDashboard.revenueTitle')}</h2>
              <p>{t('pages.adminDashboard.revenueSubtitle')}</p>
            </div>

            {stats.revenueByMonth.length === 0 ? (
              <EmptyState
                description={t('pages.adminDashboard.revenueEmptyDescription')}
                title={t('pages.adminDashboard.revenueEmptyTitle')}
              />
            ) : (
              <div className="admin-revenue-list">
                {stats.revenueByMonth.map((revenue) => (
                  <div className="admin-revenue-item" key={`${revenue.year}-${revenue.month}-${revenue.currency}`}>
                    <p className="admin-revenue-month">{getMonthLabel(revenue, language)}</p>
                    <p className="admin-revenue-amount">{formatRevenueValue(revenue, language)}</p>
                    <Badge variant="accent">
                      {t('pages.adminDashboard.revenuePaidOrders', { count: revenue.paidOrders })}
                    </Badge>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </>
      ) : null}
    </Container>
  );
}
