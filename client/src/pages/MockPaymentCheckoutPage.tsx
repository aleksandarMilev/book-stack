import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, EmptyState } from '@/components/ui';
import { paymentsApi } from '@/features/payments/api/payments.api';
import { paymentSessionStorage } from '@/features/payments/storage/paymentSession.storage';
import { ROUTES } from '@/routes/paths';
import { redirectTo } from '@/utils/navigation';

type MockPaymentOutcome = 'success' | 'failed' | 'canceled' | 'pending';

const toWebhookStatus = (outcome: MockPaymentOutcome): string => {
  if (outcome === 'success') {
    return 'paid';
  }

  if (outcome === 'failed') {
    return 'failed';
  }

  if (outcome === 'canceled') {
    return 'canceled';
  }

  return 'processing';
};

const createEventId = (): string => `mock_event_${crypto.randomUUID()}`;

export function MockPaymentCheckoutPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const sessionId = searchParams.get('sessionId') ?? '';
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const orderId = useMemo(() => {
    const queryOrderId = searchParams.get('orderId');
    if (queryOrderId) {
      return queryOrderId;
    }

    return paymentSessionStorage.getPendingOrderId() ?? '';
  }, [searchParams]);

  const goToPaymentReturn = (outcome: MockPaymentOutcome): void => {
    const returnQuery = new URLSearchParams({
      status: outcome,
      ...(orderId ? { orderId } : {}),
    });

    redirectTo(`${ROUTES.paymentReturn}?${returnQuery.toString()}`);
  };

  const handleOutcome = async (outcome: MockPaymentOutcome): Promise<void> => {
    if (!sessionId) {
      setErrorMessage(t('pages.mockPayment.missingSessionId'));
      return;
    }

    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await paymentsApi.sendMockWebhook({
        eventId: createEventId(),
        paymentSessionId: sessionId,
        status: toWebhookStatus(outcome),
      });

      if (outcome === 'success' && orderId) {
        paymentSessionStorage.clearGuestPaymentToken(orderId);
      }

      paymentSessionStorage.clearPendingOrderId();
      goToPaymentReturn(outcome);
    } catch (error: unknown) {
      setErrorMessage(getApiErrorMessage(error, t('pages.mockPayment.submitError')));
      setIsSubmitting(false);
    }
  };

  if (!sessionId) {
    return (
      <Container className="placeholder-page">
        <EmptyState
          description={t('pages.mockPayment.missingSessionId')}
          title={t('pages.mockPayment.invalidSessionTitle')}
        />
      </Container>
    );
  }

  return (
    <Container className="mock-payment-page">
      <Card className="mock-payment-card" elevated>
        <h1>{t('pages.mockPayment.title')}</h1>
        <p>{t('pages.mockPayment.subtitle')}</p>
        <dl className="mock-payment-metadata">
          <dt>{t('pages.mockPayment.sessionIdLabel')}</dt>
          <dd>{sessionId}</dd>
          <dt>{t('pages.mockPayment.orderIdLabel')}</dt>
          <dd>{orderId || '-'}</dd>
        </dl>

        <div className="mock-payment-actions">
          <Button
            disabled={isSubmitting}
            onClick={() => {
              void handleOutcome('success');
            }}
          >
            {t('pages.mockPayment.actions.success')}
          </Button>
          <Button
            disabled={isSubmitting}
            onClick={() => {
              void handleOutcome('failed');
            }}
            variant="secondary"
          >
            {t('pages.mockPayment.actions.failed')}
          </Button>
          <Button
            disabled={isSubmitting}
            onClick={() => {
              void handleOutcome('canceled');
            }}
            variant="ghost"
          >
            {t('pages.mockPayment.actions.canceled')}
          </Button>
          <Button
            disabled={isSubmitting}
            onClick={() => {
              void handleOutcome('pending');
            }}
            variant="ghost"
          >
            {t('pages.mockPayment.actions.pending')}
          </Button>
        </div>

        {errorMessage ? <p className="auth-error">{errorMessage}</p> : null}
      </Card>
    </Container>
  );
}
