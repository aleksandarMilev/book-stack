import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import { Badge, Button, Card, Container, LoadingState } from '@/components/ui';
import { checkoutService } from '@/features/checkout/services/checkout.service';
import { ordersApi } from '@/features/orders/api/orders.api';
import type { UserOrder } from '@/features/orders/types';
import { paymentSessionStorage } from '@/features/payments/storage/paymentSession.storage';
import { ROUTES } from '@/routes/paths';
import { useAuthCapabilities } from '@/store/auth.store';
import { classNames } from '@/utils/classNames';
import { redirectTo } from '@/utils/navigation';

type PaymentReturnOutcome = 'processing' | 'success' | 'failed' | 'canceled';

const parseOutcome = (value: string | null): PaymentReturnOutcome | null => {
  if (!value) {
    return null;
  }

  const normalizedValue = value.trim().toLowerCase();
  if (normalizedValue === 'success' || normalizedValue === 'paid') {
    return 'success';
  }

  if (normalizedValue === 'failed' || normalizedValue === 'failure') {
    return 'failed';
  }

  if (normalizedValue === 'canceled' || normalizedValue === 'cancelled') {
    return 'canceled';
  }

  if (normalizedValue === 'processing' || normalizedValue === 'pending') {
    return 'processing';
  }

  return null;
};

const mapOrderPaymentStatusToOutcome = (order: UserOrder): PaymentReturnOutcome => {
  if (order.paymentStatus === 'paid' || order.paymentStatus === 'refunded' || order.paymentStatus === 'notRequired') {
    return 'success';
  }

  if (order.paymentStatus === 'failed') {
    return 'failed';
  }

  if (order.paymentStatus === 'cancelled' || order.paymentStatus === 'expired') {
    return 'canceled';
  }

  return 'processing';
};

const getOutcomeBadgeVariant = (
  outcome: PaymentReturnOutcome,
): 'neutral' | 'success' | 'warning' | 'danger' => {
  if (outcome === 'success') {
    return 'success';
  }

  if (outcome === 'processing') {
    return 'warning';
  }

  if (outcome === 'failed') {
    return 'danger';
  }

  return 'neutral';
};

const getPaymentStatusBadgeVariant = (
  paymentStatus: UserOrder['paymentStatus'],
): 'success' | 'warning' | 'danger' => {
  if (paymentStatus === 'paid' || paymentStatus === 'notRequired') {
    return 'success';
  }

  if (paymentStatus === 'failed' || paymentStatus === 'cancelled' || paymentStatus === 'expired') {
    return 'danger';
  }

  return 'warning';
};

export function PaymentReturnPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const capabilities = useAuthCapabilities();
  const orderId = searchParams.get('orderId');
  const queryOutcome = parseOutcome(searchParams.get('status'));

  const [order, setOrder] = useState<UserOrder | null>(null);
  const [isLoadingOrder, setIsLoadingOrder] = useState(false);
  const [orderLoadError, setOrderLoadError] = useState<string | null>(null);
  const [retryError, setRetryError] = useState<string | null>(null);
  const [isRetrying, setIsRetrying] = useState(false);
  const [reloadCounter, setReloadCounter] = useState(0);

  useEffect(() => {
    if (queryOutcome === 'success' && orderId) {
      paymentSessionStorage.clearGuestPaymentToken(orderId);
      paymentSessionStorage.clearPendingOrderId();
    }
  }, [orderId, queryOutcome]);

  useEffect(() => {
    if (!capabilities.isAuthenticated || !orderId) {
      return;
    }

    let isActive = true;

    const loadOrder = async (): Promise<void> => {
      setIsLoadingOrder(true);
      setOrderLoadError(null);

      try {
        const response = await ordersApi.getMyOrderDetails(orderId);
        if (!isActive) {
          return;
        }

        setOrder(response);
      } catch (error: unknown) {
        if (!isActive) {
          return;
        }

        setOrder(null);
        setOrderLoadError(getApiErrorMessage(error, t('pages.paymentReturn.orderLoadError')));
      } finally {
        if (isActive) {
          setIsLoadingOrder(false);
        }
      }
    };

    void loadOrder();

    return () => {
      isActive = false;
    };
  }, [capabilities.isAuthenticated, orderId, reloadCounter, t]);

  const effectiveOutcome = useMemo<PaymentReturnOutcome>(() => {
    if (order) {
      return mapOrderPaymentStatusToOutcome(order);
    }

    return queryOutcome ?? 'processing';
  }, [order, queryOutcome]);

  const canRetryPayment =
    Boolean(orderId) &&
    effectiveOutcome !== 'success' &&
    (!order || (order.paymentMethod === 'online' && order.paymentStatus !== 'notRequired'));
  const hasOrderReference = Boolean(orderId);
  const marketplaceActionVariant: 'primary' | 'secondary' | 'ghost' = canRetryPayment
    ? 'ghost'
    : capabilities.isAuthenticated
      ? 'secondary'
      : 'primary';
  const myOrdersActionVariant: 'primary' | 'secondary' = canRetryPayment ? 'secondary' : 'primary';

  const handleRetryPayment = async (): Promise<void> => {
    if (!orderId) {
      return;
    }

    setRetryError(null);
    setIsRetrying(true);

    try {
      const checkoutResult = await checkoutService.startCheckoutForOrder(orderId, capabilities.isAuthenticated);
      redirectTo(checkoutResult.checkoutUrl);
    } catch (error: unknown) {
      setRetryError(getApiErrorMessage(error, t('pages.paymentReturn.retryError')));
      setIsRetrying(false);
    }
  };

  const statusTitleKey = `pages.paymentReturn.outcomes.${effectiveOutcome}.title`;
  const statusDescriptionKey = `pages.paymentReturn.outcomes.${effectiveOutcome}.description`;

  return (
    <Container className="payment-return-page conversion-result-page payment-return-page--result">
      <Card
        className={classNames(
          'payment-return-card',
          'conversion-surface-card',
          'conversion-result-card',
          'payment-return-card--result',
          `payment-return-card--${effectiveOutcome}`,
        )}
        elevated
      >
        <header className="conversion-result-hero">
          <Badge className="conversion-result-badge" variant={getOutcomeBadgeVariant(effectiveOutcome)}>
            {t(`pages.paymentReturn.outcomes.${effectiveOutcome}.badge`)}
          </Badge>
          <h1>{t(statusTitleKey)}</h1>
          <p className="conversion-result-description">{t(statusDescriptionKey)}</p>
        </header>

        <section aria-label={t('pages.paymentReturn.summaryTitle')} className="conversion-result-summary">
          <h2>{t('pages.paymentReturn.summaryTitle')}</h2>

          {isLoadingOrder ? <LoadingState title={t('pages.paymentReturn.loadingOrderTitle')} /> : null}

          {!isLoadingOrder && order ? (
            <>
              <div className="conversion-result-summary-row">
                <span className="conversion-result-summary-label">{t('pages.paymentReturn.orderIdLabel')}</span>
                <strong className="conversion-result-summary-value">{order.id}</strong>
              </div>
              <div className="conversion-result-summary-row">
                <span className="conversion-result-summary-label">{t('pages.paymentReturn.paymentMethodLabel')}</span>
                <Badge variant="neutral">{t(`taxonomy.paymentMethod.${order.paymentMethod}`)}</Badge>
              </div>
              <div className="conversion-result-summary-row">
                <span className="conversion-result-summary-label">{t('pages.paymentReturn.orderStatusLabel')}</span>
                <Badge variant="accent">{t(`taxonomy.orderStatus.${order.status}`)}</Badge>
              </div>
              <div className="conversion-result-summary-row">
                <span className="conversion-result-summary-label">{t('pages.paymentReturn.paymentStatusLabel')}</span>
                <Badge variant={getPaymentStatusBadgeVariant(order.paymentStatus)}>
                  {t(`taxonomy.paymentStatus.${order.paymentStatus}`)}
                </Badge>
              </div>
              <div className="conversion-result-summary-row payment-return-summary-row--total">
                <span className="conversion-result-summary-label">{t('pages.paymentReturn.orderTotalLabel')}</span>
                <PriceDisplay value={order.total} />
              </div>
            </>
          ) : null}

          {!isLoadingOrder && !order && hasOrderReference ? (
            <div className="conversion-result-summary-row">
              <span className="conversion-result-summary-label">{t('pages.paymentReturn.orderIdLabel')}</span>
              <strong className="conversion-result-summary-value">{orderId}</strong>
            </div>
          ) : null}

          {!isLoadingOrder && !order && !hasOrderReference ? (
            <p className="conversion-result-summary-note payment-return-summary-note">{t('pages.paymentReturn.noOrderHint')}</p>
          ) : null}
        </section>

        <section aria-label={t('pages.paymentReturn.supportTitle')} className="conversion-result-reassurance">
          <Badge className="conversion-result-reassurance-badge" variant="neutral">
            {t('common.labels.trustedMarketplace')}
          </Badge>
          <p>{t('pages.paymentReturn.supportMessage')}</p>

          {!capabilities.isAuthenticated && hasOrderReference ? (
            <p className="payment-return-note">{t('pages.paymentReturn.guestOrderHint')}</p>
          ) : null}

          {orderLoadError ? (
            <>
              <p className="auth-error">{orderLoadError}</p>
              <Button
                onClick={() => {
                  setReloadCounter((previousCounter) => previousCounter + 1);
                }}
                variant="secondary"
              >
                {t('common.actions.retry')}
              </Button>
            </>
          ) : null}

          {retryError ? <p className="auth-error">{retryError}</p> : null}
        </section>

        <section aria-label={t('pages.paymentReturn.nextStepsTitle')} className="conversion-result-next-steps">
          <p className="conversion-result-next-steps-title">
            {t('pages.paymentReturn.nextStepsTitle')}
          </p>

          <div className="payment-return-actions conversion-result-actions">
            {canRetryPayment ? (
              <Button
                disabled={isRetrying}
                onClick={() => {
                  void handleRetryPayment();
                }}
              >
                {isRetrying ? t('pages.paymentReturn.retrying') : t('pages.paymentReturn.retryAction')}
              </Button>
            ) : null}

            {capabilities.isAuthenticated ? (
              <Link to={ROUTES.myOrders}>
                <Button variant={myOrdersActionVariant}>{t('pages.paymentReturn.myOrdersAction')}</Button>
              </Link>
            ) : null}

            <Link to={ROUTES.marketplace}>
              <Button variant={marketplaceActionVariant}>{t('pages.paymentReturn.marketplaceAction')}</Button>
            </Link>
          </div>
        </section>
      </Card>
    </Container>
  );
}
