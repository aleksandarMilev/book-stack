import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';

import { Badge, Button, Card, Container } from '@/components/ui';
import { ROUTES } from '@/routes/paths';
import { useAuthCapabilities } from '@/store/auth.store';

type ConfirmationPaymentMethod = 'cashOnDelivery' | 'online' | null;

const parsePaymentMethod = (value: string | null): ConfirmationPaymentMethod => {
  if (!value) {
    return null;
  }

  const normalizedValue = value.trim().toLowerCase();
  if (normalizedValue === 'cashondelivery') {
    return 'cashOnDelivery';
  }

  if (normalizedValue === 'online') {
    return 'online';
  }

  return null;
};

export function OrderConfirmationPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const capabilities = useAuthCapabilities();

  const orderId = searchParams.get('orderId');
  const paymentMethod = parsePaymentMethod(searchParams.get('paymentMethod'));
  const paymentMethodKey = paymentMethod ?? 'cashOnDelivery';
  const statusKey = paymentMethodKey === 'online' ? 'pendingPayment' : 'pendingConfirmation';

  return (
    <Container className="payment-return-page conversion-result-page order-confirmation-page">
      <Card className="payment-return-card conversion-surface-card conversion-result-card order-confirmation-card" elevated>
        <header className="conversion-result-hero">
          <Badge className="conversion-result-badge" variant="success">
            {t('pages.orderConfirmation.badge')}
          </Badge>
          <h1>{t('pages.orderConfirmation.title')}</h1>
          <p className="conversion-result-description">
            {t(`pages.orderConfirmation.descriptions.${paymentMethodKey}`)}
          </p>
        </header>

        <section aria-label={t('pages.orderConfirmation.summaryTitle')} className="conversion-result-summary">
          <h2>{t('pages.orderConfirmation.summaryTitle')}</h2>

          <div className="conversion-result-summary-row">
            <span className="conversion-result-summary-label">{t('pages.orderConfirmation.orderIdLabel')}</span>
            {orderId ? (
              <strong className="conversion-result-summary-value">{orderId}</strong>
            ) : (
              <span className="conversion-result-summary-note order-confirmation-summary-note">
                {t('pages.orderConfirmation.missingOrderReference')}
              </span>
            )}
          </div>

          <div className="conversion-result-summary-row">
            <span className="conversion-result-summary-label">{t('pages.orderConfirmation.paymentMethodLabel')}</span>
            <Badge variant="neutral">{t(`taxonomy.paymentMethod.${paymentMethodKey}`)}</Badge>
          </div>

          <div className="conversion-result-summary-row">
            <span className="conversion-result-summary-label">{t('pages.orderConfirmation.orderStatusLabel')}</span>
            <Badge variant={paymentMethodKey === 'online' ? 'warning' : 'accent'}>{t(`taxonomy.orderStatus.${statusKey}`)}</Badge>
          </div>
        </section>

        <div className="conversion-result-reassurance">
          <Badge className="conversion-result-reassurance-badge" variant="neutral">
            {t('common.labels.trustedMarketplace')}
          </Badge>
          <p>{t('pages.orderConfirmation.reassurance')}</p>
        </div>

        <section aria-label={t('pages.orderConfirmation.nextStepsTitle')} className="conversion-result-next-steps">
          <p className="conversion-result-next-steps-title">
            {t('pages.orderConfirmation.nextStepsTitle')}
          </p>

          <div className="payment-return-actions conversion-result-actions">
            {capabilities.isAuthenticated ? (
              <Link to={ROUTES.myOrders}>
                <Button>{t('pages.orderConfirmation.myOrdersAction')}</Button>
              </Link>
            ) : null}

            <Link to={ROUTES.marketplace}>
              <Button variant={capabilities.isAuthenticated ? 'secondary' : 'primary'}>
                {t('pages.orderConfirmation.marketplaceAction')}
              </Button>
            </Link>
          </div>
        </section>
      </Card>
    </Container>
  );
}
