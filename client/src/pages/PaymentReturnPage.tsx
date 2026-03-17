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
  if (order.paymentStatus === 'paid' || order.paymentStatus === 'refunded') {
    return 'success';
  }

  if (order.paymentStatus === 'failed') {
    return 'failed';
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
  }, [capabilities.isAuthenticated, orderId, t]);

  const effectiveOutcome = useMemo<PaymentReturnOutcome>(() => {
    if (order) {
      return mapOrderPaymentStatusToOutcome(order);
    }

    return queryOutcome ?? 'processing';
  }, [order, queryOutcome]);

  const canRetryPayment = Boolean(orderId) && effectiveOutcome !== 'success';

  const handleRetryPayment = async (): Promise<void> => {
    if (!orderId) {
      return;
    }

    setRetryError(null);
    setIsRetrying(true);

    try {
      const checkoutResult = await checkoutService.startCheckoutForOrder(orderId, capabilities.isAuthenticated, {
        provider: 'mock',
      });
      redirectTo(checkoutResult.checkoutUrl);
    } catch (error: unknown) {
      setRetryError(getApiErrorMessage(error, t('pages.paymentReturn.retryError')));
      setIsRetrying(false);
    }
  };

  const statusTitleKey = `pages.paymentReturn.outcomes.${effectiveOutcome}.title`;
  const statusDescriptionKey = `pages.paymentReturn.outcomes.${effectiveOutcome}.description`;

  return (
    <Container className="payment-return-page">
      <Card className="payment-return-card" elevated>
        <Badge variant={getOutcomeBadgeVariant(effectiveOutcome)}>
          {t(`pages.paymentReturn.outcomes.${effectiveOutcome}.badge`)}
        </Badge>
        <h1>{t(statusTitleKey)}</h1>
        <p>{t(statusDescriptionKey)}</p>

        {isLoadingOrder ? (
          <LoadingState title={t('pages.paymentReturn.loadingOrderTitle')} />
        ) : null}

        {!isLoadingOrder && order ? (
          <div className="payment-return-order-summary">
            <p className="payment-return-order-id">
              {t('pages.paymentReturn.orderIdLabel')}: {order.id}
            </p>
            <div className="payment-return-order-statuses">
              <Badge variant="accent">{t(`taxonomy.orderStatus.${order.status}`)}</Badge>
              <Badge variant={order.paymentStatus === 'paid' ? 'success' : 'warning'}>
                {t(`taxonomy.paymentStatus.${order.paymentStatus}`)}
              </Badge>
            </div>
            <div className="payment-return-price">
              <span>{t('pages.paymentReturn.orderTotalLabel')}</span>
              <PriceDisplay value={order.total} />
            </div>
          </div>
        ) : null}

        {orderLoadError ? <p className="auth-error">{orderLoadError}</p> : null}
        {retryError ? <p className="auth-error">{retryError}</p> : null}

        <div className="payment-return-actions">
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
              <Button variant="secondary">{t('pages.paymentReturn.myOrdersAction')}</Button>
            </Link>
          ) : null}

          <Link to={ROUTES.marketplace}>
            <Button variant="ghost">{t('pages.paymentReturn.marketplaceAction')}</Button>
          </Link>
        </div>
      </Card>
    </Container>
  );
}
